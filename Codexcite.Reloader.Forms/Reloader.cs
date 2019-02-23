using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Polly;
using Polly.Retry;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Codexcite.Reloader.Forms
{
	public static class Reloader
	{
		private const string HubPath = "/reloadhub";
		private static CancellationTokenSource _reloaderCancellationTokenSource;
		private static readonly Dictionary<string, string> _cachedUpdatedPages = new Dictionary<string, string>();
		private static bool _isManuallyReappearing = false;
		private static string AppClass;

		private static readonly AsyncRetryPolicy RetryPolicy = Policy
			.Handle<Exception>()
			.WaitAndRetryForeverAsync(
				retryAttempt => TimeSpan.FromSeconds(5),
				(exception, timespan) =>
				{
					Debug.WriteLine($"Reloader: Disconnected '{exception.Message}' Time: '{timespan}'. Retrying to connect.");
				});

		private static Page _currentPage;

		public static void Stop()
		{
			_reloaderCancellationTokenSource?.Cancel();
		}

		public static void Init(string url)
		{
			AppClass = Application.Current.GetType().FullName;

			if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
				throw new ArgumentException("The url must be a valid absolute url address", nameof(url));
			var fullUrl = new Uri(new Uri(url, UriKind.Absolute), HubPath);

			_reloaderCancellationTokenSource = new CancellationTokenSource();

			Debug.WriteLine($"Reloader: Connecting to '{fullUrl}'.");

			var connection = new HubConnectionBuilder()
				.WithUrl(fullUrl)
				.Build();

			connection.Closed += async error =>
			{
				Debug.WriteLine($"Reloader: Disconnected '{error.Message}'. Retrying...");
				if (!_reloaderCancellationTokenSource.IsCancellationRequested)
					await RetryPolicy.ExecuteAsync(() => connection.StartAsync()).ConfigureAwait(false);
			};

			connection.On<byte[]>("ReloadXaml", contents =>
			{
				if (contents == null)
				{
					Debug.WriteLine("Reloader: Received empty content.");
					return;
				}

				var xaml = Encoding.UTF8.GetString(contents);

				var xamlClass = xaml.ExtractClassName();
				Debug.WriteLine($"Reloader: Received xaml for '{xamlClass}'.'");
				UpdateCachedPage(xamlClass, xaml);

				if (AppClass == xamlClass)
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						Debug.WriteLine($"Reloader: Updating the App.xaml resources.'");
						Application.Current.Resources.Clear();
						Application.Current.LoadFromXaml(xaml);
					});
				}
				else if (_currentPage != null && _currentPage is ContentPage contentPage)
				{
					var currentPageClass = _currentPage.GetType().FullName;
					if (xamlClass == currentPageClass)
						UpdatePageXaml(contentPage, xaml);
				}
			});

			Application.Current.PageAppearing += ApplicationOnPageAppearing;


			var task = connection.StartAsync(_reloaderCancellationTokenSource.Token);
			Debug.WriteLine($"Reloader: Connected to '{fullUrl}'.");
		}

		private static void UpdatePageXaml(ContentPage contentPage, string xaml)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				Debug.WriteLine($"Reloader: Updating current page '{_currentPage.GetType().FullName}'.'");

				contentPage.Content = null;
				contentPage.LoadFromXaml(xaml);
				ReassignNamedElements(contentPage);
				try
				{
					_isManuallyReappearing = true;
					// using Xamarin.Forms internal methods; must fire both in order to get the ReactiveUI WhenActivated to be called
					contentPage.SendDisappearing();
					contentPage.SendAppearing();
					// second option, using reflection to raise the events
					//contentPage.Raise(typeof(Page), nameof(Page.Disappearing), EventArgs.Empty);
					//contentPage.Raise(typeof(Page), nameof(Page.Appearing), EventArgs.Empty);

				}
				finally
				{
					_isManuallyReappearing = false;
				}
			});
		}

		private static void ReassignNamedElements(Element element)
		{
			var fields = element.GetType()
				.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(f => f.IsDefined(typeof(GeneratedCodeAttribute), true));

			foreach (var field in fields)
			{
				var value = element.FindByName<object>(field.Name);
				field.SetValue(element, value);
			}
		}

		private static void ApplicationOnPageAppearing(object sender, Page e)
		{
			_currentPage = e is NavigationPage navigationPage ? navigationPage.CurrentPage : e;
			if (!_isManuallyReappearing)
			{
				var updatedXaml = GetCachedPageXaml(_currentPage.GetType().FullName);
				if (updatedXaml != null && _currentPage is ContentPage contentPage)
					UpdatePageXaml(contentPage, updatedXaml);
			}
		}

		private static void UpdateCachedPage(string className, string xaml)
		{
			_cachedUpdatedPages[className] = xaml;
		}

		private static string GetCachedPageXaml(string className)
		{
			return _cachedUpdatedPages.ContainsKey(className) ? _cachedUpdatedPages[className] : null;
		}
	}
}
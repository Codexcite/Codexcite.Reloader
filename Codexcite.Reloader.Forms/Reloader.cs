using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Codexcite.Reloader.Forms
{
	public static class Reloader
	{
		private const string PING = "PING";
		private static CancellationTokenSource _reloaderCancellationTokenSource;
		private static readonly Dictionary<string, string> _cachedUpdatedPages = new Dictionary<string, string>();
		private static bool _isManuallyReappearing = false;
		private static string AppClass;

		private static Page _currentPage;
		private static Client _client;
		private static IDisposable _subscription;

		public static void Stop()
		{
			_reloaderCancellationTokenSource?.Cancel();
			_subscription?.Dispose();
		}

		public static void Init(string host, int port)
		{
			AppClass = Application.Current.GetType().FullName;

			_reloaderCancellationTokenSource = new CancellationTokenSource();
			Application.Current.PageAppearing += ApplicationOnPageAppearing;


			Debug.WriteLine($"Reloader: Connecting to '{host}:{port}'.");

			_client = new Client(host, port, _reloaderCancellationTokenSource.Token);

			_subscription = _client.ReadMessageObservable
				.Subscribe(HandleReceivedXaml,
					exception =>
					{
						Debug.WriteLine($"Reloader: Exception '{exception.Message}'.");
					},
					() =>
					{
						Debug.WriteLine($"Reloader: DONE!'.");
					});
		}

		private static void HandleReceivedXaml(byte[] contents)
		{
			if (contents == null)
			{
				Debug.WriteLine("Reloader: Received empty content.");
				return;
			}

			var xaml = Encoding.UTF8.GetString(contents);
			HandleReceivedXaml(xaml);
		}


		private static void HandleReceivedXaml(string xaml)
		{
			if (string.IsNullOrEmpty(xaml))
			{
				Debug.WriteLine("Reloader: Received empty xaml.");
				return;
			}
			if (xaml == PING)
			{
				//Debug.WriteLine("Reloader: Received PING.");
				return;
			}

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
				if (xamlClass == currentPageClass) UpdatePageXaml(contentPage, xaml);
			}
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
			if (e is NavigationPage navigationPage)
			{
				_currentPage = navigationPage.CurrentPage;
			}
			else
				_currentPage = e;
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
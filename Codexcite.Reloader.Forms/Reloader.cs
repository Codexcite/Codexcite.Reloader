using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Codexcite.Reloader.Forms
{
	public static class Reloader
	{
		private const string HubPath = "/reloadhub";
		private static CancellationTokenSource _reloaderCancellationTokenSource;

		public static void Stop()
		{
			_reloaderCancellationTokenSource?.Cancel();
		}

		public static void Init(string url)
		{
			if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
				throw new ArgumentException("The url must be a valid absolute url address", nameof(url));
			var fullUrl = new Uri(new Uri(url, UriKind.Absolute), HubPath);

			_reloaderCancellationTokenSource = new CancellationTokenSource();

			Debug.WriteLine($"Reloader: Connecting to '{fullUrl}'.");

			var connection = new HubConnectionBuilder()
				.WithUrl(fullUrl)
				.Build();

			connection.Closed += async (error) =>
			{
				Debug.WriteLine($"Reloader: Disconnected '{error.Message}'.");
				if (!_reloaderCancellationTokenSource.IsCancellationRequested)
				{
					await Task.Delay(new Random().Next(0, 5) * 1000);
					await connection.StartAsync();
				}
			};

			connection.On<byte[]>("ReloadXaml", (contents) =>
			{
				if (contents == null)
				{
					Debug.WriteLine("Reloader: Received empty content.");
					return;
				}
				var xaml = Encoding.UTF8.GetString(contents);


				if (_currentPage != null && _currentPage is ContentPage contentPage)
				{
					var xamlClass = xaml.ExtractClassName();
					var currentPageClass = _currentPage.GetType().FullName;
					Debug.WriteLine($"Reloader: Received xaml for '{xamlClass}'. Current page is '{currentPageClass}.'");
					if (xamlClass == currentPageClass)
					{
						Device.BeginInvokeOnMainThread(() =>
						{
							Debug.WriteLine($"Reloader: Updating current page '{currentPageClass}'.'");

							contentPage.Content = null;
							contentPage.LoadFromXaml(xaml);
							ReassignNamedElements(contentPage);
							
						});

					}
				}
			});

			Application.Current.PageAppearing += ApplicationOnPageAppearing;


			var task = connection.StartAsync(_reloaderCancellationTokenSource.Token);
			Debug.WriteLine($"Reloader: Connected to '{fullUrl}'.");

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

		private static Page _currentPage = null;
		private static void ApplicationOnPageAppearing(object sender, Page e)
		{
			_currentPage = e is NavigationPage navigationPage ? navigationPage.CurrentPage : e;
		}
	}
}

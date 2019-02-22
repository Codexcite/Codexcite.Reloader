using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Security.Permissions;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Codexcite.Reloader.Monitor
{
	public class Program
	{
		private static readonly string[] SupportedFileExtensions = { "xaml" };
		private static IWebHost _webHost;

		private static readonly AsyncRetryPolicy<string> RetryPolicy = Policy<string>
			.Handle<IOException>()
			.WaitAndRetryAsync(new[]
			{
				TimeSpan.FromMilliseconds(100),
				TimeSpan.FromMilliseconds(200),
				TimeSpan.FromMilliseconds(300),
			});

		[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
		public static void Main(string[] args)
		{
			var path = args.GetCommandLineArgument("-path", Environment.CurrentDirectory);

			var fileMonitorObservable = GetFileMonitorObservable(path); 
			if (fileMonitorObservable == null)	
				return;

			fileMonitorObservable.Subscribe(SendFileUpdate);

			var url = args.GetCommandLineArgument("-url", null);
			if (url == null)
			{
				var host = args.GetCommandLineArgument("-host", null) ?? GetDefaultPrivateNetworkIp();
				var port = args.GetCommandLineArgument("-port", "5500");
				url = $"http://{host}:{port}";
			}
			Console.WriteLine($"Starting host on '{url}'.");
			_webHost = CreateWebHostBuilder(args).UseUrls(url).Build();
			_webHost.Run();
			Console.WriteLine($"Running on '{url}'.");

		}

		private static string GetDefaultPrivateNetworkIp()
		{
			return NetworkInterface
				       .GetAllNetworkInterfaces()
				       .SelectMany(x => x.GetIPProperties().UnicastAddresses)
				       .Where(x=> x.SuffixOrigin != SuffixOrigin.LinkLayerAddress && 
				                  x.Address.AddressFamily == AddressFamily.InterNetwork)
				       .Select(x => x.Address.MapToIPv4())
				       .FirstOrDefault(x => x.ToString() != "127.0.0.1")?.ToString() ?? "127.0.0.1";
		}

		private static IObservable<string> GetFileMonitorObservable(string path)
		{
			if (!Directory.Exists(path))
			{
				Console.WriteLine("Invalid folder to monitor. Use '-p [path] command argument to set folder path.'");
				return null;
			}

			Console.WriteLine($"Watching folder: '{path}'");
			IObservable<string> result = null;
			foreach (var fileExtension in SupportedFileExtensions)
			{
				var watcher = new FileSystemWatcher
				{
					Path = path,
					NotifyFilter = NotifyFilters.LastWrite |
					               NotifyFilters.Attributes |
					               NotifyFilters.Size |
					               NotifyFilters.CreationTime |
					               NotifyFilters.FileName,
					Filter = $"*.{fileExtension}",
					EnableRaisingEvents = true,
					IncludeSubdirectories = true
				};

				var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						x => watcher.Changed += x,
						x => watcher.Changed -= x)
					.Select(x => x.EventArgs.FullPath);
				var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						x => watcher.Created += x,
						x => watcher.Created -= x)
					.Select(x => x.EventArgs.FullPath);
				var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
						x => watcher.Renamed += x,
						x => watcher.Renamed -= x)
					.Select(x => x.EventArgs.FullPath);

				var merged = changed.Merge(created).Merge(renamed);
				result = result == null ? merged : result.Merge(merged);
			}
			return result?.Throttle(TimeSpan.FromMilliseconds(100));
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>();

		

		private static async void SendFileUpdate(string filePath)
		{
			Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - File updated: '{filePath}'");
			try
			{
				string fileContent = await RetryPolicy.ExecuteAsync(() => File.ReadAllTextAsync(filePath));

				if (fileContent.Length == 0)
				{
					Console.WriteLine($"Empty file");
					return;
				}

				var data = Encoding.UTF8.GetBytes(fileContent);
				if (data == null || data.Length == 0)
				{
					Console.WriteLine($"Empty file");
					return;
				}
				var hubContext = _webHost.Services
					.GetRequiredService<IHubContext<ReloadHub>>();
				Console.WriteLine($"Sending: {data.Length} bytes from file '{filePath}'.");
				await hubContext.Clients.All.SendAsync("ReloadXaml", data).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				Console.WriteLine($"ERROR: {e}.");
			}
		}
	}
}

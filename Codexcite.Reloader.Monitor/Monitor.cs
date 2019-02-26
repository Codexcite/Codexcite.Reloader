using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using Polly;
using Polly.Retry;

namespace Codexcite.Reloader.Monitor
{
	public class Monitor
	{
		private static readonly string[] SupportedFileExtensions = { "xaml" };

		private static readonly AsyncRetryPolicy<string> AsyncRetryPolicy = Policy<string>
			.Handle<IOException>()
			.WaitAndRetryAsync(new[]
			{
				TimeSpan.FromMilliseconds(100),
				TimeSpan.FromMilliseconds(200),
				TimeSpan.FromMilliseconds(300),
			});
		private static readonly RetryPolicy<string> RetryPolicy = Policy<string>
			.Handle<IOException>()
			.WaitAndRetry(new[]
			{
				TimeSpan.FromMilliseconds(100),
				TimeSpan.FromMilliseconds(200),
				TimeSpan.FromMilliseconds(300),
			});
		private Server _server;
		private IDisposable _fileMonitorSubscription;

		public event EventHandler<EventArgs<string>> ClientConnected;
		public event EventHandler<EventArgs<string>> ClientDisconnected;
		public event EventHandler<EventArgs<string>> Error;
		protected virtual void OnClientConnected(string data)
		{
			ClientConnected?.Invoke(this, new EventArgs<string>(data));
		}

		protected virtual void OnClientDisconnected(string data)
		{
			ClientDisconnected?.Invoke(this, new EventArgs<string>(data));
		}
		protected virtual void OnError(string data)
		{
			Error?.Invoke(this, new EventArgs<string>(data));
		}

		public void Stop()
		{
			_server?.Stop();
			_fileMonitorSubscription?.Dispose();
		}

		public bool Start(string path, string host, int port)
		{
			var actualHost = host ?? GetDefaultPrivateNetworkIp();
			Debug.WriteLine($"Starting host on '{actualHost}:{port}'.");
			_server = new Server();
			var serverStarted = _server.Start(actualHost, port);
			if (!serverStarted)
			{
				_server.Dispose();
				_server = null;
				return false;
			}
			Debug.WriteLine($"Running on '{actualHost}:{port}'.");

			_server.ClientConnected += (sender, args) => OnClientConnected(args.Data);
			_server.ClientDisconnected += (sender, args) => OnClientDisconnected(args.Data);
			_server.Error += (sender, args) => OnError(args.Data);

			var fileMonitorObservable = GetFileMonitorObservable(path);
			if (fileMonitorObservable != null)
			{
				_fileMonitorSubscription = fileMonitorObservable.Subscribe(SendFileUpdate);
				return true;
			}

			_server?.Stop();
			_server.Dispose();
			_server = null;
			return false;
		}

		public static string GetDefaultPrivateNetworkIp()
		{
			return NetworkInterface
							 .GetAllNetworkInterfaces()
							 .SelectMany(x => x.GetIPProperties().UnicastAddresses)
							 .Where(x => x.SuffixOrigin != SuffixOrigin.LinkLayerAddress &&
													x.Address.AddressFamily == AddressFamily.InterNetwork)
							 .Select(x => x.Address.MapToIPv4())
							 .FirstOrDefault(x => x.ToString() != "127.0.0.1")?.ToString() ?? "127.0.0.1";
		}

		private IObservable<string> GetFileMonitorObservable(string path)
		{
			if (!Directory.Exists(path))
			{
				Debug.WriteLine("Invalid folder to monitor. Use '-p [path] command argument to set folder path.'");
				OnError($"Invalid folder to monitor. '{path}' is not a valid path.'");
				return null;
			}

			Debug.WriteLine($"Watching folder: '{path}'");
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

		private async void SendFileUpdate(string filePath)
		{
			Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - File updated: '{filePath}'");
			try
			{
				string fileContent = RetryPolicy.Execute(() => File.ReadAllText(filePath));

				if (fileContent.Length == 0)
				{
					Debug.WriteLine($"Empty file");
					return;
				}

				var data = Encoding.UTF8.GetBytes(fileContent);
				if (data == null || data.Length == 0)
				{
					Debug.WriteLine($"Empty file");
					return;
				}
				Debug.WriteLine($"Sending: {data.Length} bytes from file '{filePath}'.");
				await _server.SendMessageToAllAsync(data);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"ERROR: {e}.");
			}
		}
	}
}

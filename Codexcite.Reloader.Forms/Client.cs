using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Xamarin.Forms;

namespace Codexcite.Reloader.Forms
{
	public class Client : IDisposable
	{
		private TcpClient _client;
		private static readonly AsyncRetryPolicy RetryPolicy = Policy
			.Handle<Exception>()
			.WaitAndRetryForeverAsync(i =>
			{
				Debug.WriteLine($"Connect failed, retry number {i}");
				return TimeSpan.FromSeconds(5);
			});
		private readonly CancellationToken _cancellationToken;
		private readonly string _host;
		private readonly int _port;

		public bool Connected => _client?.Connected ?? false;
		public IObservable<string> ReadMessageObservable { get; private set; }

		public Client(string host, int port, CancellationToken cancellationToken)
		{
			_host = host;
			_port = port;
			_cancellationToken = cancellationToken;

			ReadMessageObservable = Observable.Interval(TimeSpan.FromMilliseconds(500))
				.Select(DoLoop)
				.Concat()
				.Where(x => x != null)
				.TakeUntil(_ => cancellationToken.IsCancellationRequested)
				.Publish().RefCount();
		}

		private IObservable<string> DoLoop(long iterationCount)
		{
			var result = Observable.If(() => !Connected
																			 || ((Device.RuntimePlatform == Device.iOS || Device.RuntimePlatform == Device.Android)
																						&& iterationCount % 60 == 0),
																	Observable.FromAsync(DoConnect))
															.Select(_ => (string)null)
															.Concat(Observable
																			.While(() => _client.Available > 0,
																											Observable.FromAsync(DoReadMessage)));
			//.Concat(Observable.Return((string) null).Delay(TimeSpan.FromMilliseconds(500)));

			return result;

		}

		private async Task<string> DoReadMessage()
		{
			try
			{
				if (!Connected || _client.Available == 0)
					return null;
				var header = new byte[4];
				//Debug.WriteLine(
				//	$"ReadMessage: start reading header '{Thread.CurrentThread.Name}' id:{Thread.CurrentThread.ManagedThreadId} pool:{Thread.CurrentThread.IsBackground}  state:{Thread.CurrentThread.ThreadState}");
				await _client.GetStream().ReadAsync(header, 0, header.Length, _cancellationToken).ConfigureAwait(false);

				var messageLength = BitConverter.ToInt32(header, 0);
				//Debug.WriteLine($"ReadMessage: end reading header {messageLength}");
				if (messageLength <= 0)
					throw new Exception($"Invalid message length in header: {messageLength}");

				var messageBuffer = new byte[messageLength];
				//Debug.WriteLine($"ReadMessage: start reading message");
				await _client.GetStream().ReadAsync(messageBuffer, 0, messageBuffer.Length, _cancellationToken).ConfigureAwait(false);

				var message = Encoding.UTF8.GetString(messageBuffer, 0, messageBuffer.Length);
				//Debug.WriteLine($"ReadMessage: end reading message {message.Length}");
				return message;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Exception while trying to ReadMessage: {e.Message}");
				return null;
			}
		}

		private bool _isConnecting;

		private async Task DoConnect()
		{
			try
			{
				if (_isConnecting || _cancellationToken.IsCancellationRequested)
					return;
				_isConnecting = true;
				if (_client != null)
					_client.Client.Disconnect(true);
				else
					_client = new TcpClient { ExclusiveAddressUse = false };

				await RetryPolicy.ExecuteAsync(() =>
					!_cancellationToken.IsCancellationRequested ? _client.ConnectAsync(_host, _port) : Task.CompletedTask);
				_isConnecting = false;

				Debug.WriteLine($"Connected... {_client.EssentialLocalInfoAsString()}");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Exception while trying to DoConnect: {e.Message}");
				throw;
			}
		}

		public void Dispose()
		{
			_client?.Client?.Disconnect(false);
			_client?.Dispose();
		}

	}
}

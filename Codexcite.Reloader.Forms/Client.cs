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
		private readonly bool _autoReconnect;

		public bool Connected => _client?.Connected ?? false;
		public IObservable<string> ReadMessageObservable { get; private set; }

		public Client(string host, int port, CancellationToken cancellationToken, bool autoReconnect = true)
		{
			_host = host;
			_port = port;
			_autoReconnect = autoReconnect;
			_cancellationToken = cancellationToken;

			ReadMessageObservable = Observable.Interval(TimeSpan.FromMilliseconds(500))
				.Select(iteration => Observable.FromAsync(()=>DoLoop(iteration)))
				.Concat()
				.Where(x => x != null)
				.Publish().RefCount();


		}

		private async Task<string> DoLoop(long iterationCount)
		{
			if (!Connected || iterationCount % 60 == 0)
				await DoConnect();
			if (_client.Available > 0)
				return await DoReadMessage();
			return null;
		}
		
		public void Stop()
		{
			//_cancellationTokenSource.Cancel();
		}
		public async Task<bool> Start()
		{
			try
			{
				await DoConnect();
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
				return false;
			}

			return true;
		}

		public async Task<string> ReadMessage()
		{
			string message = null;
			try
			{
				//if (_client?.Client != null)
				//{
				//	_client.Client.Disconnect(true);
				//	await _client.Client.ConnectAsync(_host, _port);
				//}
				if (_client == null)
					await Start().ConfigureAwait(false);
				if (!Connected)
				{
					Debug.WriteLine("Disconnected...");
				}
				else
				{
					message = await DoReadMessage();
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Exception while trying to ReadMessage: {e.Message}");
			}

			if (message == null && !Connected && _autoReconnect)
			{
				await Start();
				return Connected ? await ReadMessage() : null;
			}

			return message;
		}

		private async Task<string> DoReadMessage()
		{
			var header = new byte[4];
			Debug.WriteLine(
				$"ReadMessage: start reading header '{Thread.CurrentThread.Name}' id:{Thread.CurrentThread.ManagedThreadId} pool:{Thread.CurrentThread.IsBackground}  state:{Thread.CurrentThread.ThreadState}");
			await _client.GetStream().ReadAsync(header, 0, header.Length, _cancellationToken).ConfigureAwait(false);

			var messageLength = BitConverter.ToInt32(header, 0);
			Debug.WriteLine($"ReadMessage: end reading header {messageLength}");
			if (messageLength <= 0)
				throw new Exception($"Invalid message length in header: {messageLength}");

			var messageBuffer = new byte[messageLength];
			Debug.WriteLine($"ReadMessage: start reading message");
			await _client.GetStream().ReadAsync(messageBuffer, 0, messageBuffer.Length, _cancellationToken).ConfigureAwait(false);

			var message = Encoding.UTF8.GetString(messageBuffer, 0, messageBuffer.Length);
			Debug.WriteLine($"ReadMessage: end reading message {message.Length}");
			return message;
		}

		private bool _isConnecting;

		private async Task DoConnect()
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

			Debug.WriteLine($"Connected... {((IPEndPoint)_client.Client.LocalEndPoint).Address}:{((IPEndPoint)_client.Client.LocalEndPoint).Port}");
		}

		public void Dispose()
		{
			_client?.Client?.Disconnect(false);
			_client?.Dispose();
		}

	}
}

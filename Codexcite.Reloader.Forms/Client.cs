using System;
using System.ComponentModel;
using System.Diagnostics;
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
	public class Client : INotifyPropertyChanged, IDisposable
	{
		private TcpClient _client;
		private static readonly AsyncRetryPolicy RetryPolicy = Policy
			.Handle<Exception>()
			.WaitAndRetryForeverAsync(i =>
			{
				Debug.WriteLine($"Connect failed, retry number {i}");
				return TimeSpan.FromSeconds(5);
			});
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly string _host;
		private readonly int _port;
		private readonly bool _autoReconnect;

		private bool _connected;
		public bool Connected
		{
			get => _connected;
			set
			{
				if (value == _connected) return;
				_connected = value;
				OnPropertyChanged();
			}
		}


		public Client(string host, int port, bool autoReconnect = true)
		{
			_host = host;
			_port = port;
			_autoReconnect = autoReconnect;
		}

		public void Stop()
		{
			_cancellationTokenSource.Cancel();
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
				if (!_client.Connected)
				{
					Debug.WriteLine("Disconnected...");
					Connected = false;
				}
				else
				{
					var header = new byte[4];
					await _client.GetStream().ReadAsync(header, 0, header.Length);

					var messageLength = BitConverter.ToInt32(header, 0);
					if (messageLength <= 0)
						throw new Exception($"Invalid message length in header: {messageLength}");

					var messageBuffer = new byte[messageLength];
					await _client.GetStream().ReadAsync(messageBuffer, 0, messageBuffer.Length);

					message = Encoding.UTF8.GetString(messageBuffer, 0, messageBuffer.Length);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Exception while trying to ReadMessage: {e.Message}");
				Connected = false;
			}

			if (message == null && !Connected && _autoReconnect)
			{
				await Start();
				return Connected ? await ReadMessage() : null;
			}

			return message;
		}

		private bool _isConnecting;

		private async Task HandleDisconnected()
		{
			if (_autoReconnect)
			{
				Debug.WriteLine("Auto reconnecting...");
				await DoConnect();
			}
		}

		private async Task DoConnect()
		{
			if (Connected || _isConnecting || _cancellationTokenSource.IsCancellationRequested)
				return;
			_isConnecting = true;
			_client?.Dispose();
			_client = new TcpClient { ExclusiveAddressUse = false };
			await RetryPolicy.ExecuteAsync(() =>
				!_cancellationTokenSource.IsCancellationRequested ? _client.ConnectAsync(_host, _port) : Task.CompletedTask);
			_isConnecting = false;
			Connected = true;
			Debug.WriteLine($"Connected... {_client.Client.Handle}");
		}

		public void Dispose()
		{
			_client?.Dispose();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

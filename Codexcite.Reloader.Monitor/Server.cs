using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codexcite.Reloader.Monitor
{
	public class Server : IDisposable
	{
		private TcpListener _tcpListener;
		private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();
		private readonly List<TcpClient> _connectedClients = new List<TcpClient>();
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private bool _isDisposed;

		public void Stop()
		{
			_cancellationTokenSource.Cancel();
		}
		public bool Start(string host, int port)
		{
			try
			{
				if (_isDisposed)
					return false;
				_tcpListener = new TcpListener(IPAddress.Parse(host), port) { ExclusiveAddressUse = false };

				_tcpListener.Start();
				var acceptObservable = Observable.FromAsync(_tcpListener.AcceptTcpClientAsync)
					.Repeat();
				acceptObservable.Subscribe(client =>
					{
						_connectedClients.Add(client);
						client.DisposeWith(_compositeDisposable);
						Console.WriteLine($"Connected {client.Client.Handle}");
					}, _cancellationTokenSource.Token);
				//.DisposeWith(_compositeDisposable);


			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}

			return true;
		}

		public async Task SendMessageToAllAsync(byte[] bytes)
		{
			if (_isDisposed)
				return;
			var headerBytes = BitConverter.GetBytes(bytes.Length);
			foreach (var client in _connectedClients.ToArray())
			{
				if (!client.Connected)
				{
					Console.WriteLine($"Disconnected {client.Client.Handle}");
					_connectedClients.Remove(client);
				}
				else
				{
					Console.WriteLine($"Sending to {client.Client.Handle} header length: {headerBytes.Length}, message length:{bytes.Length}");
					try
					{
						await client.GetStream().WriteAsync(headerBytes, 0, headerBytes.Length);
						await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
					}
					catch (Exception e)
					{
						Console.WriteLine($"Disconnected {client.Client.Handle} - {e.Message}");
						_connectedClients.Remove(client);
					}
				}
			}
		}

		public async Task SendMessageToAllAsync(string message)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			await SendMessageToAllAsync(bytes);
		}
		public void Dispose()
		{
			if (_isDisposed)
				return;
			_isDisposed = true;
			_tcpListener?.Stop();
			_compositeDisposable?.Dispose();

		}
	}
}

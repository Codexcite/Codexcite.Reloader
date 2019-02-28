using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private const string PING = "PING";
		private TcpListener _tcpListener;
		private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();
		private readonly List<TcpClient> _connectedClients = new List<TcpClient>();
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private bool _isDisposed;

		public event EventHandler<EventArgs<string>> ClientConnected;
		public event EventHandler<EventArgs<string>> ClientDisconnected;
		public event EventHandler<EventArgs<string>> Error;

		public int ConnectedClientsCount => _connectedClients.Count;

		public void Stop()
		{
			_cancellationTokenSource.Cancel();
			_tcpListener?.Stop();
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
						Trace.WriteLine($"Connected {client.EssentialInfoAsString()}");
						OnClientConnected(client);
					}, _cancellationTokenSource.Token);
				//.DisposeWith(_compositeDisposable);
				Observable.FromAsync(() => SendMessageToAllAsync(PING))
					.RepeatAfterDelay(TimeSpan.FromSeconds(5))
					.Subscribe(_cancellationTokenSource.Token);

			}
			catch (Exception e)
			{
				Trace.WriteLine(e);
				OnError(e.Message);
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
					Trace.WriteLine($"Disconnected {client.EssentialInfoAsString()}");
					_connectedClients.Remove(client);
					OnClientDisconnected(client);
				}
				else
				{
					Trace.WriteLine($"Sending to {client.EssentialInfoAsString()} header length: {headerBytes.Length}, message length:{bytes.Length}");
					try
					{
						await client.GetStream().WriteAsync(headerBytes, 0, headerBytes.Length);
						await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
					}
					catch (Exception e)
					{
						Trace.WriteLine($"Disconnected {client.EssentialInfoAsString()} - {e.Message}");
						_connectedClients.Remove(client);
						OnClientDisconnected(client);
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
			foreach (var client in _connectedClients)
			{
				client.Client.Disconnect(false);
			}
			_tcpListener?.Stop();
			_compositeDisposable?.Dispose();

		}

		protected virtual void OnClientConnected(TcpClient client)
		{
			ClientConnected?.Invoke(this, new EventArgs<string>(client.EssentialInfoAsString()));
		}

		protected virtual void OnClientDisconnected(TcpClient client)
		{
			ClientDisconnected?.Invoke(this, new EventArgs<string>(client.EssentialInfoAsString()));
		}
		protected virtual void OnError(string data)
		{
			Error?.Invoke(this, new EventArgs<string>(data));
		}

	}
}

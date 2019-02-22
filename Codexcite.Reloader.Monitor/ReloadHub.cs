using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Codexcite.Reloader.Monitor
{
	public class ReloadHub : Hub
	{
		public override async Task OnConnectedAsync()
		{
			Console.WriteLine($"Connected {Context.ConnectionId}");
			await base.OnConnectedAsync();
		}

		public override Task OnDisconnectedAsync(Exception exception)
		{
			Console.WriteLine($"Disconnected {Context.ConnectionId}");

			return base.OnDisconnectedAsync(exception);

		}
	}
}

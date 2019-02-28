using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Codexcite.Reloader.Monitor
{
	public static class Extensions
	{
		public static string GetCommandLineArgument(this string[] args, string key, string defaultValue)
		{
			var keyPosition = Array.IndexOf(args, key);
			if (keyPosition < 0 || keyPosition == args.Length - 1)
				return defaultValue;
			return args[keyPosition + 1];
		}

		public static string ExtractClassName(this string xaml)
			=> Regex.Match(xaml, "x:Class=\"(.+)\"").Groups[1].Value;

		public static IObservable<T> RepeatAfterDelay<T>(this IObservable<T> source, TimeSpan delay, IScheduler scheduler)
		{
			var repeatSignal = Observable
				.Empty<T>()
				.Delay(delay, scheduler);

			// when source finishes, wait for the specified
			// delay, then repeat.
			return source.Concat(repeatSignal).Repeat();
		}
		public static IObservable<T> RepeatAfterDelay<T>(this IObservable<T> source, TimeSpan delay)
		{
			var repeatSignal = Observable
				.Empty<T>()
				.Delay(delay);

			// when source finishes, wait for the specified
			// delay, then repeat.
			return source.Concat(repeatSignal).Repeat();
		}
		public static string EssentialInfoAsString(this TcpClient client)
		{
			return
				$"{client?.Client?.Handle} - {(client?.Client?.RemoteEndPoint as IPEndPoint)?.Address}:{(client?.Client?.RemoteEndPoint as IPEndPoint)?.Port}";
		}
	}
}

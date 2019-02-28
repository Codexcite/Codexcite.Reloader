using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Codexcite.Reloader.Forms
{
	public static class Extensions
	{
	

		public static string ExtractClassName(this string xaml)
			=> Regex.Match(xaml, "x:Class=\"(.+)\"").Groups[1].Value;

		internal static void Raise<TEventArgs>(this object source, Type type, string eventName, TEventArgs eventArgs) where TEventArgs : EventArgs
		{
			var eventProp = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
			var eventDelegate = (MulticastDelegate)eventProp?.GetValue(source);
			if (eventDelegate != null)
			{
				foreach (var handler in eventDelegate.GetInvocationList())
				{
					handler.Method.Invoke(handler.Target, new object[] { source, eventArgs });
				}
			}
		}

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
		public static string EssentialLocalInfoAsString(this TcpClient client)
		{
			return
				$"{client?.Client?.Handle} - {(client?.Client?.LocalEndPoint as IPEndPoint)?.Address}:{(client?.Client?.LocalEndPoint as IPEndPoint)?.Port}";
		}
	}
}

using System;
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
	}
}

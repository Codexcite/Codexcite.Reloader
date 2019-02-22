using System;
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
	}
}

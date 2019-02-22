using System;
using System.Text.RegularExpressions;

namespace Codexcite.Reloader.Forms
{
	public static class Extensions
	{
	

		public static string ExtractClassName(this string xaml)
			=> Regex.Match(xaml, "x:Class=\"(.+)\"").Groups[1].Value;
	}
}

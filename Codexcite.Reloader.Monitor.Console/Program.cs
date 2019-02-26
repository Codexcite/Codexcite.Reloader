using System;
using System.Diagnostics;
using System.IO;

namespace Codexcite.Reloader.Monitor.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			TextWriterTraceListener myWriter = new 
				TextWriterTraceListener(System.Console.Out);
			Trace.Listeners.Add(myWriter);
			var path = args.GetCommandLineArgument("-path", Environment.CurrentDirectory);
			var host = args.GetCommandLineArgument("-host", null);
			int.TryParse(args.GetCommandLineArgument("-port", "5500"), out int port);

			var monitor = new Monitor();

			var started = monitor.Start(path, host, port);

			System.Console.WriteLine("Press Ctrl+C to stop...");
			do
			{
				
			} while (started);
		}
	}
}

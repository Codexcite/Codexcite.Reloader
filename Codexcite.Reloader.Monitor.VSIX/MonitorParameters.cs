using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codexcite.Reloader.Monitor.VSIX
{
	public class MonitorParameters
	{
		public string Path { get; set; }
		public string Host { get; set; }
		public string Port { get; set; }

		public int PortAsInt => int.TryParse(Port, out int p) ? p : 0;

		public bool IsValid => !string.IsNullOrEmpty(Path) && !string.IsNullOrEmpty(Host) && PortAsInt > 0;
	}
}

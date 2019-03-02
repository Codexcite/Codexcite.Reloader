using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Codexcite.Reloader.Monitor.VSIX
{
	public static class OutputWindow
	{
		private static bool _isFirstTime = true;
		public static void Write(string message)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
			Guid generalPaneGuid = VSConstants.GUID_OutWindowDebugPane; // P.S. There's also the GUID_OutWindowDebugPane available.
			IVsOutputWindowPane generalPane = null;
			outWindow?.GetPane(ref generalPaneGuid, out generalPane);
			generalPane?.OutputString(Environment.NewLine);
			generalPane?.OutputString(message);
			generalPane?.OutputString(Environment.NewLine);
			if (_isFirstTime)
			{
				_isFirstTime = false;
				generalPane?.Activate();
			}
		}
	}
}

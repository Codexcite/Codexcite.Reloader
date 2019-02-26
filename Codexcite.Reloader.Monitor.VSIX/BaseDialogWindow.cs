using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.PlatformUI;

namespace Codexcite.Reloader.Monitor.VSIX
{

	public class BaseDialogWindow : DialogWindow
	{

		public BaseDialogWindow()
		{
			this.HasMaximizeButton = false;
			this.HasMinimizeButton = false;
		}
	}
}

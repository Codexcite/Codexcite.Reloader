using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Codexcite.Reloader.Monitor.VSIX
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class StartMonitorCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("7016decd-a70f-481f-8bfb-bab77379925a");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="StartMonitorCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private StartMonitorCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			menuItem.Checked = ReloaderPackage.Monitor != null;
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static StartMonitorCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Switch to the main thread - the call to AddCommand in StartMonitorCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			Instance = new StartMonitorCommand(package, commandService);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (ReloaderPackage.Monitor != null)
			{
				ReloaderPackage.Monitor.Stop();
				ReloaderPackage.Monitor = null;
				string message = $"Reloader Monitor stopped.";

				OutputWindow.Write(message);

				if (sender is MenuCommand command)
					command.Checked = false;
				return;
			}

			var initialParameters = GetSavedParameters();
			var parameters = ShowMonitorParametersWindow(initialParameters);

			if (parameters != null)
			{
				Settings.Default.Path = parameters.Path;
				Settings.Default.Host = parameters.Host;
				Settings.Default.Port = parameters.Port;
				Settings.Default.Save();

				if (parameters.IsValid)
				{
					ReloaderPackage.Monitor = new Monitor();
					var started = ReloaderPackage.Monitor.Start(parameters.Path, parameters.Host, parameters.PortAsInt);

					if (!started)
						ReloaderPackage.Monitor = null;


					string message = started ? $"Started Reloader Monitor: path={parameters?.Path}; host={parameters?.Host}; port={parameters?.Port}"
																			: $"Reloader Monitor did NOT start.";
					//string title = "Reloader Monitor";

					OutputWindow.Write(message);

					if (sender is MenuCommand command)
						command.Checked = started;
					//// Show a message box to prove we were here
					//VsShellUtilities.ShowMessageBox(
					//	this.package,
					//	message,
					//	title,
					//	OLEMSGICON.OLEMSGICON_INFO,
					//	OLEMSGBUTTON.OLEMSGBUTTON_OK,
					//	OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				}
			}


		}

		

		private MonitorParameters ShowMonitorParametersWindow(MonitorParameters initialParameters)
		{
			var documentationControl = new MonitorParametersWindow(initialParameters);
			var ok = documentationControl.ShowDialog();
			if (ok.HasValue && ok.Value)
			{
				return new MonitorParameters
				{
					Host = documentationControl.Host,
					Path = documentationControl.Path,
					Port = documentationControl.Port
				};
			}

			return null;
		}

		private MonitorParameters GetSavedParameters()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var parameters = new MonitorParameters()
			{
				Path = Settings.Default.Path,
				Host = Settings.Default.Host,
				Port = Settings.Default.Port,
			};
			if (string.IsNullOrEmpty(parameters.Path))
			{
				var dte = (DTE)this.ServiceProvider?.GetServiceAsync(typeof(DTE))?.Result;
				if (!string.IsNullOrEmpty(dte?.Solution?.FullName))
				{
					string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
					parameters.Path = solutionDir;
				}
				else
				{
					parameters.Path = Environment.CurrentDirectory;
				}
			}

			if (string.IsNullOrEmpty(parameters.Host))
				parameters.Host = Monitor.GetDefaultPrivateNetworkIp();

			if (string.IsNullOrEmpty(parameters.Port))
				parameters.Port = "5500";

			return parameters;
		}
	}
}

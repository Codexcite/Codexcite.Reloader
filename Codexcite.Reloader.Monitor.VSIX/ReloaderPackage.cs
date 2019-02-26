using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace Codexcite.Reloader.Monitor.VSIX
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[Guid(ReloaderPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class ReloaderPackage : AsyncPackage
	{
		private static Monitor _monitor;

		/// <summary>
		/// ReloaderPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "635d0521-5e85-49e8-bccc-b5443c49588f";

		public static Monitor Monitor
		{
			get => _monitor;
			set
			{
				if (_monitor == value)
					return;
				if (_monitor != null)
				{
					_monitor.ClientConnected -= MonitorOnClientConnected;
					_monitor.ClientDisconnected -= MonitorOnClientDisconnected;
					_monitor.Error -= MonitorOnError;
				}
				_monitor = value;
				if (_monitor != null)
				{
					_monitor.ClientConnected += MonitorOnClientConnected;
					_monitor.ClientDisconnected += MonitorOnClientDisconnected;
					_monitor.Error += MonitorOnError;
				}
			}
		}

		public static void MonitorOnClientConnected(object sender, EventArgs<string> e)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					OutputWindow.Write($"Reloader Monitor: Client connected {e.Data}");
				}
			);
		}
		public static void MonitorOnClientDisconnected(object sender, EventArgs<string> e)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				OutputWindow.Write($"Reloader Monitor: Client disconnected {e.Data}");
			});
		}
		public static void MonitorOnError(object sender, EventArgs<string> e)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				OutputWindow.Write($"Reloader Monitor: Error {e.Data}");
			});
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReloaderPackage"/> class.
		/// </summary>
		public ReloaderPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			await Codexcite.Reloader.Monitor.VSIX.StartMonitorCommand.InitializeAsync(this);
		}

		#endregion
	}
}

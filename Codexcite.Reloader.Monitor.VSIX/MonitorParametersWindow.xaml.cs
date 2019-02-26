using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Codexcite.Reloader.Monitor.VSIX
{
	/// <summary>
	/// Interaction logic for MonitorParametersWindow.xaml
	/// </summary>
	public partial class MonitorParametersWindow : BaseDialogWindow
	{
		public MonitorParametersWindow(MonitorParameters initialValues = null)
		{
			InitializeComponent();
			if (initialValues != null)
			{
				TextBoxPath.Text = initialValues.Path;
				TextBoxHost.Text = initialValues.Host;
				TextBoxPort.Text = initialValues.Port;
			}
		}

		public string Path => TextBoxPath.Text;
		public string Host => TextBoxHost.Text;
		public string Port => TextBoxPort.Text;

		private void Save_OnClick(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}

		private void Cancel_OnClick(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void Browse_OnClick(object sender, RoutedEventArgs e)
		{
			using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
			{
				if (!string.IsNullOrEmpty(Path))
					dialog.SelectedPath = Path;
				System.Windows.Forms.DialogResult result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
					TextBoxPath.Text = dialog.SelectedPath;
			}
		}
		private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
		private static bool IsTextAllowed(string text)
		{
			return !_regex.IsMatch(text);
		}

		private void TextBoxPort_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsTextAllowed(e.Text);
		}

		private void TextBoxPort_OnPasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(String)))
			{
				String text = (String)e.DataObject.GetData(typeof(String));
				if (!IsTextAllowed(text))
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}
	}
}

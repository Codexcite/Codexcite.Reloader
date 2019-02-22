using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Codexcite.Reloader.Sample.App
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
			BindingContext = new MainViewModel(Navigation);
		}

		private int _count = 0;
		private void Button_OnClicked(object sender, EventArgs e)
		{
			LabelCount.Text = $"Count: {_count++}";
		}
	}
}

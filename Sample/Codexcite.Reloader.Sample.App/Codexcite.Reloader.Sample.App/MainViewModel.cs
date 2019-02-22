using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Codexcite.Reloader.Sample.App
{
	public class MainViewModel
	{
		private static int _instanceCount = 0;

		public MainViewModel(INavigation navigation)
		{
			_instanceCount++;
			GoToSecond = new Command(() => navigation.PushAsync(new SecondPage()));
		}

		public ICommand GoToSecond { get; }
		public string[] Values => new[] {"Uno", "Due", "Tri", "Citiri"};
		public int InstanceCount => _instanceCount;
	}
}

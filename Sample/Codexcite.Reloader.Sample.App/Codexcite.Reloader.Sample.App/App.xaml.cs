using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Codexcite.Reloader.Sample.App
{
	public partial class App : Application
	{
		public App()
		{
			#if DEBUG
			// TODO: Update the url with your ip and port
			Reloader.Forms.Reloader.Init("192.168.1.128", 5500);
			#endif
			InitializeComponent();

			MainPage = new NavigationPage(new MainPage());
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}

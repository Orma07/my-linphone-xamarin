using LibLinphone.Interfaces;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace MyLinphoneAppTes
{
	public partial class App : Application
	{
        public static ILinphoneManager LinphoneManager { get; private set; }
        public App ()
		{
			InitializeComponent();
            LinphoneManager = DependencyService.Get<ILinphoneManager>();
            MainPage = new NavigationPage(new MainPage());
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}

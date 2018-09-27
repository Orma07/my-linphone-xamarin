using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using LibLinphone.Droid.LinphoneUtils;
using Java.IO;

namespace MyLinphoneAppTes.Droid
{
    [Activity(Label = "MyLinphoneAppTest", 
        Icon = "@mipmap/icon", 
        Theme = "@style/MainTheme",
        MainLauncher = true, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            LinphoneManagerAndroid.Start();
            LoadApplication(new App()); 

            var folder = new File("/certs/");

            var files = folder.ListFiles();

            File certificate = null;

            if (files != null)
            {
                foreach (var fileEntry in files)
                {
                    System.Diagnostics.Debug.WriteLine("Certificate name: " + fileEntry.Name);
                    System.Diagnostics.Debug.WriteLine("Certificate can read: " + fileEntry.CanRead().ToString());


                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("certs is null");
            }

        }
    }
}


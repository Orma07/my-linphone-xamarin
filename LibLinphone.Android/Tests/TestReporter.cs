

using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Java.Lang;
using LibLinphone.Android.Tests;
using LibLinphone.forms.Interfaces;
using System.IO;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(TestReporter))]
namespace LibLinphone.Android.Tests
{

    public class TestReporter : ITestReport
    {


        public void SaveLog(int id)
        {
            var filePath = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads) + $"/log_call_{id}";
            Runtime.GetRuntime().Exec(new string[]
            {
                "logcat",
                "-f",
                filePath,
            });
        }

        public async Task TakeScreen(int id)
        {
            var view = (Activity)Xamarin.Forms.Forms.Context;
            try
            {
                var image = view.Window.DecorView.RootView;
                image.DrawingCacheEnabled = true;
                var bitmap = Bitmap.CreateBitmap(image.DrawingCache);
                image.DrawingCacheEnabled = false;
                var file = File.Create(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads) + $"/image_call_{id}");
                int quality = 100;
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, quality, file);
                file.Flush();
                file.Close();
            }
            catch(Exception ex)
            {
                Log.Error("TestReporter", $"Couldn't take screenshot -> ex: {ex.Message}");
            }
        }
    }
}
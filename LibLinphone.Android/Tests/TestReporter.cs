

using System;
using Android.App;
using Android.Graphics;
using Android.Util;
using Java.Lang;
using LibLinphone.Android.Tests;
using LibLinphone.forms.Interfaces;
using System.IO;
using System.Threading.Tasks;
using Environment = Android.OS.Environment;
using Exception = Java.Lang.Exception;

[assembly: Xamarin.Forms.Dependency(typeof(TestReporter))]
namespace LibLinphone.Android.Tests
{

    public class TestReporter : ITestReport
    {


        public void SaveLog(int id)
        {
            try
            {
                Log.Debug("TEST_REPORT", $"Call numer: {id}");
                var filePath = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads) + $"/debug_call_log.txt";
                File.Delete(filePath);
                Runtime.GetRuntime().Exec(new string[]
                {
                    "logcat",
                    "-f",
                    filePath,
                });
            }
            catch (Exception e)
            {
                Log.Error("TEST_REPORT", "exception while saving log: " + e.StackTrace);
            }
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
                var file = File.Create(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads) + $"/image_call.jpeg");
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
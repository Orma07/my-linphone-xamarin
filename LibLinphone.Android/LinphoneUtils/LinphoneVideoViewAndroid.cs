using System;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Runtime;
using Android.Util;
using Android.Views;
using LibLinphone.Android.LinphoneUtils;
using LibLinphone.Droid.LinphoneUtils;
using LibLinphone.Views;
using Org.Linphone.Mediastream.Video;
using Xamarin.Forms.Platform.Android;

[assembly: Xamarin.Forms.ExportRenderer(typeof(LinphoneVideoView), typeof(LinphoneVideoViewAndroid))]
namespace LibLinphone.Droid.LinphoneUtils
{
    public class LinphoneVideoViewAndroid : ViewRenderer<LinphoneVideoView, View>
    {
        Org.Linphone.Mediastream.Video.Display.GL2JNIView captureCamera;
        //SurfaceView captureCamera;
        AndroidVideoWindowImpl androidView;
        private MediaRecorder mMediaRecorder;
        private Context Context { get; set; }
        public LinphoneVideoViewAndroid(Context context) : base(context)
        {
            Context = context;
        }

        private const double scaleFactor = 1;

        private void InitAndroidView(int width, int height)
        {
            captureCamera = new Org.Linphone.Mediastream.Video.Display.GL2JNIView(Context);
            //captureCamera = new SurfaceView(Context);
            var displayMetrics = new DisplayMetrics();
            var ctx = Application.Context;
            var windowManager = ctx.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            windowManager.DefaultDisplay.GetMetrics(displayMetrics);
            //var currentActivity = (Activity)Forms.Context;
            //var Display = currentActivity.WindowManager.DefaultDisplay;
            //var size = new Android.Graphics.Point();
            //Display.GetSize(size);
            int width_android = (int)(width / (1f / displayMetrics.Density));
            int height_android = (int)(height / (1f / displayMetrics.Density));
            captureCamera.Holder.SetFixedSize(width_android, height_android);
            ViewGroup.LayoutParams cparams = new ViewGroup.LayoutParams(width_android, height_android);
            captureCamera.LayoutParameters = cparams;

            androidView = new AndroidVideoWindowImpl(captureCamera, null, null);

            androidView.SetListener(new AndroidVideoWindowListener { VideoHandle = androidView.Handle });

            captureCamera.SetZOrderOnTop(false);
            captureCamera.SetZOrderMediaOverlay(true);

            
            
            LinphoneEngineAndroid.Instance.LinphoneCore.VideoDisplayEnabled = true;


        }


        private Xamarin.Forms.StackLayout s;
        private void PopulateView()
        {
            Element.SizeChanged -= SizeChanged_Event;
            Element.SizeChanged += SizeChanged_Event;
        }

        private void Recvideo()
        {
            //mMediaRecorder.set
        }

        private void SizeChanged_Event(object sender, EventArgs e)
        {
            if (Element.Width > 0 && Element.Height > 0)
            {
                InitAndroidView((int)Element.Width, (int)Element.Height);

                s = new Xamarin.Forms.StackLayout();
                s.IsEnabled = false;
                s.InputTransparent = true;
                Element.InputTransparent = true;
                Element.ClearingView -= ClearView;
                Element.ClearingView += ClearView;
                Element.ZoomEvent -= OnZoomView;
                Element.ZoomEvent += OnZoomView;
                Element.IsEnabled = false;
                s.HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                s.VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                captureCamera.SetOnTouchListener(null);
                s.Children.Add(captureCamera);
                Element.Content = s;
            }
        }

        private void OnZoomView(object sender, ZoomEventsArg e)
        {
            var scale = scaleFactor;
            if (e.ZoomType == ZoomType.ZoomIn)
            {
                scale = 0.5;
            }
            var currentActivity = (Activity)Xamarin.Forms.Forms.Context;
            var Display = currentActivity.WindowManager.DefaultDisplay;
            var size = new Point();
            Display.GetSize(size);
            int width = (int)(size.X * scale);
            int height = (int)(size.Y * scale);
            captureCamera.Holder.SetFixedSize(width, height);

            // s.ScaleTo(scale);


            //captureCamera.LayoutParameters = new LayoutParams(width, height);

        }

        protected override void OnElementChanged(ElementChangedEventArgs<LinphoneVideoView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
            {
                PopulateView();
            }

        }

        private void ClearView(object sender, EventArgs e)
        {
            InitAndroidView((int)Element.Width, (int)Element.Height);
            //PopulateView();

            s = new Xamarin.Forms.StackLayout();
            s.IsEnabled = false;
            s.InputTransparent = true;
            Element.InputTransparent = true;
            Element.ClearingView -= ClearView;
            Element.ClearingView += ClearView;
            Element.ZoomEvent -= OnZoomView;
            Element.ZoomEvent += OnZoomView;
            Element.IsEnabled = false;
            s.HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
            s.VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
            captureCamera.SetOnTouchListener(null);
            s.Children.Add(captureCamera);
            Element.Content = s;
        }
    }

    public class AndroidVideoWindowListener : Java.Lang.Object,  AndroidVideoWindowImpl.IVideoWindowListener
    {
        public IntPtr VideoHandle { get; set; }

        public void OnVideoPreviewSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {

        }

        public void OnVideoPreviewSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {

        }

        public void OnVideoRenderingSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {
            LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = IntPtr.Zero;
        }

        public void OnVideoRenderingSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {
            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = VideoHandle;
            
        }
    }
}
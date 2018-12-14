using System;

using Android.App;
using Android.Content;
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
        private AndroidVideoWindowListener androidVideoWindowListener;
        public LinphoneVideoViewAndroid(Context context) : base(context)
        {

        }

        private const double scaleFactor = 1;

        private void InitAndroidView(int width, int height)
        {
            captureCamera = new Org.Linphone.Mediastream.Video.Display.GL2JNIView(Context);

            var displayMetrics = new DisplayMetrics();
            var ctx = Application.Context;
            var windowManager = ctx.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            windowManager.DefaultDisplay.GetMetrics(displayMetrics);

            int width_android = (int)(width / (1f / displayMetrics.Density));
            int height_android = (int)(height / (1f / displayMetrics.Density));
            captureCamera.Holder.SetFixedSize(width_android, height_android);
            ViewGroup.LayoutParams cparams = new ViewGroup.LayoutParams(width_android, height_android);
            captureCamera.LayoutParameters = cparams;

            androidView = new AndroidVideoWindowImpl(captureCamera, null, null);

            androidVideoWindowListener = new AndroidVideoWindowListener();
            androidVideoWindowListener.OnVideoRenderingSurfaceReadyEvent += OnVideoRenderingSurfaceReady;
            androidVideoWindowListener.OnVideoRenderingSurfaceDestroyedEvent += OnVideoRenderingSurfaceDestroyed;


            androidView.SetListener(androidVideoWindowListener);

            captureCamera.SetZOrderOnTop(false);
            captureCamera.SetZOrderMediaOverlay(true);
           
        }

        private void OnVideoRenderingSurfaceDestroyed(object sender, AndroidVideoWindowListenerArgs e)
        {
            try
            {
                androidView.Release();
                androidView.Dispose();
            }
            catch
            {

            }
        }

        private void OnVideoRenderingSurfaceReady(object sender, AndroidVideoWindowListenerArgs e)
        {
            captureCamera = e.surfaceView as Org.Linphone.Mediastream.Video.Display.GL2JNIView;
            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
            {
                captureCamera = e.surfaceView as Org.Linphone.Mediastream.Video.Display.GL2JNIView;
                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = e.androidVideoWindowImpl.Handle;
            }
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            if(LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId != IntPtr.Zero)
            {
                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = IntPtr.Zero;
            }

        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
            {
               // LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = androidView.Handle;
            }
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
            //TODO: https://gitlab.linphone.org/BC/public/linphone-android/blob/master/src/android/org/linphone/call/CallVideoFragment.java
        }

        protected override void OnElementChanged(ElementChangedEventArgs<LinphoneVideoView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
            {
                PopulateView();
            }

        }

       
    }

    public class AndroidVideoWindowListenerArgs: EventArgs
    {
        public AndroidVideoWindowImpl androidVideoWindowImpl { get;  set; }
        public SurfaceView surfaceView { get;  set; }
    }

    public class AndroidVideoWindowListener : Java.Lang.Object,  AndroidVideoWindowImpl.IVideoWindowListener
    {
        public event EventHandler<AndroidVideoWindowListenerArgs> OnVideoRenderingSurfaceDestroyedEvent;
        public event EventHandler<AndroidVideoWindowListenerArgs> OnVideoRenderingSurfaceReadyEvent;

        public void OnVideoPreviewSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {

        }

        public void OnVideoPreviewSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {

        }

        public void OnVideoRenderingSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {
            OnVideoRenderingSurfaceDestroyedEvent?.Invoke(
               this,
               new AndroidVideoWindowListenerArgs
               {
                   androidVideoWindowImpl = p0,
               });
        }

        public void OnVideoRenderingSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {
            OnVideoRenderingSurfaceReadyEvent?.Invoke(
                this,
                new AndroidVideoWindowListenerArgs
                {
                    androidVideoWindowImpl = p0,
                    surfaceView = p1
                });
        }
    }
}
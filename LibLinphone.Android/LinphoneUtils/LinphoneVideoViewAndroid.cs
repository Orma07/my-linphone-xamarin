using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using LibLinphone.Android.LinphoneUtils;
using LibLinphone.forms.Views;
using Org.Linphone.Mediastream.Video;
using Org.Linphone.Mediastream.Video.Display;
using Xamarin.Forms.Platform.Android;

[assembly: Xamarin.Forms.ExportRenderer(typeof(LinphoneVideoView), typeof(LinphoneVideoViewAndroid))]
namespace LibLinphone.Android.LinphoneUtils
{
    public class LinphoneVideoViewAndroid : ViewRenderer<LinphoneVideoView, View>
    {
        private GL2JNIView captureCamera;

        private AndroidVideoWindowImpl androidView;
        private AndroidVideoWindowListener androidVideoWindowListener;
        //private SurfaceView captureCamera;
        //private MediaRecorder mMediaRecorder;

        public LinphoneVideoViewAndroid(Context context) : base(context) { }

        private void InitAndroidView(int width, int height)
        {
            captureCamera = new GL2JNIView(Context);

            var displayMetrics = new DisplayMetrics();
            var ctx = Application.Context;
            var windowManager = ctx.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            windowManager.DefaultDisplay.GetMetrics(displayMetrics);

            var widthAndroid = (int)(width / (1f / displayMetrics.Density));
            var heightAndroid = (int)(height / (1f / displayMetrics.Density));
            captureCamera.Holder.SetFixedSize(widthAndroid, heightAndroid);
            
            var cparams = new LayoutParams(widthAndroid, heightAndroid);
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
            catch(Exception ex)
            {
                Utils.TraceException(ex);
            }
        }

        private void OnVideoRenderingSurfaceReady(object sender, AndroidVideoWindowListenerArgs e)
        {
            captureCamera = e.SurfaceView as GL2JNIView;
            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
            {
                captureCamera = e.SurfaceView as GL2JNIView;
                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = e.AndroidVideoWindowImpl.Handle;
            }
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            if(LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId != IntPtr.Zero)
                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = IntPtr.Zero;
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

        /*private void Recvideo()
        {
            //mMediaRecorder.set
        }*/

        private void SizeChanged_Event(object sender, EventArgs e)
        {
            if (Element.Width > 0 && Element.Height > 0)
            {
                InitAndroidView((int)Element.Width, (int)Element.Height);

                s = new Xamarin.Forms.StackLayout
                {
                    IsEnabled = false, 
                    InputTransparent = true
                };
                
                Element.InputTransparent = true;
                //Element.ZoomEvent -= OnZoomView;
                //Element.ZoomEvent += OnZoomView;
                Element.IsEnabled = false;
                s.HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                s.VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                captureCamera.SetOnTouchListener(null);
                s.Children.Add(captureCamera);
                Element.Content = s;
            }
        }

        /*private void OnZoomView(object sender, ZoomEventsArg e)
        {
            //TODO: https://gitlab.linphone.org/BC/public/linphone-android/blob/master/src/android/org/linphone/call/CallVideoFragment.java
        }*/

        protected override void OnElementChanged(ElementChangedEventArgs<LinphoneVideoView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
                PopulateView();
        }
    }

    public class AndroidVideoWindowListenerArgs: EventArgs
    {
        public AndroidVideoWindowImpl AndroidVideoWindowImpl { get;  set; }
        public SurfaceView SurfaceView { get;  set; }
    }

    public class AndroidVideoWindowListener : Java.Lang.Object,  AndroidVideoWindowImpl.IVideoWindowListener
    {
        public event EventHandler<AndroidVideoWindowListenerArgs> OnVideoRenderingSurfaceDestroyedEvent;
        public event EventHandler<AndroidVideoWindowListenerArgs> OnVideoRenderingSurfaceReadyEvent;

        public void OnVideoPreviewSurfaceDestroyed(AndroidVideoWindowImpl p0) { }

        public void OnVideoPreviewSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1) { }

        public void OnVideoRenderingSurfaceDestroyed(AndroidVideoWindowImpl p0)
        {
            OnVideoRenderingSurfaceDestroyedEvent?.Invoke(
               this,
               new AndroidVideoWindowListenerArgs
               {
                   AndroidVideoWindowImpl = p0,
               });
        }

        public void OnVideoRenderingSurfaceReady(AndroidVideoWindowImpl p0, SurfaceView p1)
        {
            OnVideoRenderingSurfaceReadyEvent?.Invoke(
                this,
                new AndroidVideoWindowListenerArgs
                {
                    AndroidVideoWindowImpl = p0,
                    SurfaceView = p1
                });
        }
    }
}
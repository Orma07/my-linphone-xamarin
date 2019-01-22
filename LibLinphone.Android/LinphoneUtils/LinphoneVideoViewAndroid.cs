using System;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using LibLinphone.Android.LinphoneUtils;
using LibLinphone.Droid.LinphoneUtils;
using LibLinphone.Views;
using Xamarin.Forms.Platform.Android;

[assembly: Xamarin.Forms.ExportRenderer(typeof(LinphoneVideoView), typeof(LinphoneVideoViewAndroid))]
namespace LibLinphone.Droid.LinphoneUtils
{
    public class LinphoneVideoViewAndroid : ViewRenderer<LinphoneVideoView, View>, TextureView.ISurfaceTextureListener
    {
        TextureView captureCamera;
        //SurfaceView captureCamera;
        //AndroidVideoWindowImpl androidView;
        //private MediaRecorder mMediaRecorder;
        //private AndroidVideoWindowListener androidVideoWindowListener;
        public LinphoneVideoViewAndroid(Context context) : base(context)
        {

        }

        private const double scaleFactor = 1;

        private void InitAndroidView(int width, int height)
        {
            var displayMetrics = new DisplayMetrics();
            var ctx = Application.Context;
            var windowManager = ctx.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            windowManager.DefaultDisplay.GetMetrics(displayMetrics);

            var widthAndroid = (int)(width / (1f / displayMetrics.Density));
            var heightAndroid = (int)(height / (1f / displayMetrics.Density));
            var cparams = new LayoutParams(widthAndroid, heightAndroid);
            
            captureCamera = new TextureView(Context) 
            {
                SurfaceTextureListener = this,
            };
            captureCamera.SetMinimumWidth(widthAndroid);
            captureCamera.SetMinimumHeight(heightAndroid);    
            captureCamera.LayoutParameters = cparams;
            //captureCamera.Holder.SetFixedSize(width_android, height_android);
            
            LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = captureCamera.Handle;
            
            //androidView = new AndroidVideoWindowImpl(captureCamera, null, null);

            //androidVideoWindowListener = new AndroidVideoWindowListener();
            //androidVideoWindowListener.OnVideoRenderingSurfaceReadyEvent += OnVideoRenderingSurfaceReady;
           // androidVideoWindowListener.OnVideoRenderingSurfaceDestroyedEvent += OnVideoRenderingSurfaceDestroyed;


            //androidView.SetListener(androidVideoWindowListener);

            //captureCamera.SetZOrderOnTop(false);
            //captureCamera.SetZOrderMediaOverlay(true);
           
        }

        /*private void OnVideoRenderingSurfaceDestroyed(object sender, AndroidVideoWindowListenerArgs e)
        {
            try
            {
                //androidView.Release();
                //androidView.Dispose();
            }
            catch
            {

            }
        }*/

//        private void OnVideoRenderingSurfaceReady(object sender, AndroidVideoWindowListenerArgs e)
//        {
//            captureCamera = e.surfaceView as Org.Linphone.Mediastream.Video.Display.GL2JNIView;
//            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
//            {
//                captureCamera = e.surfaceView as Org.Linphone.Mediastream.Video.Display.GL2JNIView;
//                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = e.androidVideoWindowImpl.Handle;
//            }
//        }

//        protected override void OnDetachedFromWindow()
//        {
//            base.OnDetachedFromWindow();
//            if(LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId != IntPtr.Zero)
//            {
//                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = IntPtr.Zero;
//            }
//
//        }

//        protected override void OnAttachedToWindow()
//        {
//            base.OnAttachedToWindow();
//            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
//            {
//               // LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = androidView.Handle;
//            }
//        }

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

                s = new Xamarin.Forms.StackLayout {IsEnabled = false, InputTransparent = true};
                Element.InputTransparent = true;
//                Element.ZoomEvent -= OnZoomView;
//                Element.ZoomEvent += OnZoomView;
                Element.IsEnabled = false;
                s.HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                s.VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                captureCamera.SetOnTouchListener(null);
                s.Children.Add(captureCamera);
                Element.Content = s;
            }
        }

//        private void OnZoomView(object sender, ZoomEventsArg e)
//        {
            //TODO: https://gitlab.linphone.org/BC/public/linphone-android/blob/master/src/android/org/linphone/call/CallVideoFragment.java
//        }

        protected override void OnElementChanged(ElementChangedEventArgs<LinphoneVideoView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
                PopulateView();
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId == IntPtr.Zero)
                LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = surface.Handle;
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            try
            {
                if (LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId != IntPtr.Zero)
                    LinphoneEngineAndroid.Instance.LinphoneCore.NativeVideoWindowId = IntPtr.Zero;

                surface.Dispose();
            }
            catch
            {
                //We do not want the app to crash randomly
            }
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            //throw new NotImplementedException();
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            //throw new NotImplementedException();
        }
    }

    /*public class AndroidVideoWindowListenerArgs: EventArgs
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
    }*/
}
using System;
using Xamarin.Forms;

namespace LibLinphone.forms.Views
{
    public class LinphoneVideoView : ContentView
    {
        public event EventHandler<ZoomEventsArg> ZoomEvent;
     
        public void ZoomView(ZoomType zoomType)
        {
            ZoomEvent?.Invoke(this, new ZoomEventsArg(zoomType));
        }
    }
    public enum ZoomType
    {
        ZoomOut, ZoomIn
    }

    public class ZoomEventsArg : EventArgs
    {
        public ZoomType ZoomType { get; private set; }

        public ZoomEventsArg(ZoomType zoomType)
        {
            ZoomType = zoomType;
        }
    }
}
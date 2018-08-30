using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace LibLinphone.Views
{
    public class LinphoneVideoView : ContentView
    {
        public event EventHandler ClearingView;
        public event EventHandler<ZoomEventsArg> ZoomEvent;
        public void ClearView()
        {
            ClearingView?.Invoke(this, EventArgs.Empty);
        }

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

using LibLinphone.Interfaces;
using LibLinphone.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyLinphoneAppTest
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OnCallPage : ContentPage
    {

        public event EventHandler RefuseCall;
        public event EventHandler AcceptCall;

        public void SetVideoView(CallPCL call, ILinphoneManager linphone)
        {
            contentViewVideo.IsVisible = true;
            contentViewVideo.Content = new LinphoneVideoView();
            linphone.SetViewCall(call);
        }
        public OnCallPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        private void OnCallClicked(object sender, EventArgs e)
        {
            AcceptCall?.Invoke(this, EventArgs.Empty);
        }

        private void OnCallRefuseClicked(object sender, EventArgs e)
        {
            RefuseCall?.Invoke(this, EventArgs.Empty);
        }

    }
}
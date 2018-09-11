using LibLinphone.Interfaces;
using LibLinphone.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MyLinphoneAppTes
{
	public partial class MainPage : ContentPage, ILinphneListenner
	{
        private ILinphoneManager LinphoneManager;
        private bool MockUser;
        private CallPCL CurrentCall;
        private CallStatePCL StateOfCurrentCall;
        //Kalpa
        public MainPage()
		{
			InitializeComponent();
            LinphoneManager = DependencyService.Get<ILinphoneManager>();
            LinphoneManager.AddLinphoneListenner(this);
		}

        public void OnCall(CallArgs e)
        {
            StateOfCurrentCall = e.CallState;
            CurrentCall = e.LCall;
            Debug.WriteLine("Call state changed: " + StateOfCurrentCall);

            call_status.Text = "Call state changed: " + StateOfCurrentCall;

            if (StateOfCurrentCall == CallStatePCL.IncomingReceived)
            {
                call.Text = "Answer Call (" + CurrentCall.UsernameCaller + ")";
                LinphoneManager.SetViewCall(CurrentCall);

            }
            else if(StateOfCurrentCall == CallStatePCL.IncomingEarlyMedia)
            {
                call.Text = "Answer Call (" + CurrentCall.UsernameCaller + ")";
            }
            else if(StateOfCurrentCall == CallStatePCL.StreamsRunning)
            {
                call.Text = "Terminate";
                if (!CurrentCall.IsVideoEnabled)
                {
                    LinphoneManager.SetViewCallOutgoing(CurrentCall);
                }
            }else if (StateOfCurrentCall == CallStatePCL.Released || StateOfCurrentCall == CallStatePCL.End ||
                StateOfCurrentCall == CallStatePCL.Released)
            {
                call.Text = "Start Call";
            }
          


        }

        public void OnRegistration(RegistrationStatePCL state, string mesage)
        {
            Debug.WriteLine("Registration state changed: " + state);

            registration_status.Text = "Registration state changed: " + state;

            if (state == RegistrationStatePCL.Ok)
            {
                register.IsEnabled = false;
                mockButton.IsVisible = false;
                stack_registrar.IsVisible = false;
                contentViewVideo.IsVisible = true;
                contentViewVideo.Content = new LinphoneVideoView();
            }
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            var domain = "f8dc7a13b724.f8dc7a13b7241512840382.ipvdesdev.vimar.cloud";
            var pwd = "_zeRaomqYkQyhPYOb5CUItSvnt6GUiLa";
            var usr = "60002";
            string imei = "imei";
            string myName = "myName";
            string serverAddr = "192.168.1.0";
            string routeAddr = "192.168.1.0";
           
            if (!MockUser)
            {
                usr = username.Text;
                pwd = password.Text;
                domain = this.domain.Text;
                serverAddr = domain;
                routeAddr = domain;
            }

            LinphoneManager.RegisterLinphone(usr,
                pwd,
                domain,
                imei,
                myName,
                serverAddr,
                routeAddr,
                MockUser);
        }

        private void OnCallClicked(object sender, EventArgs e)
        {
            string toCall = "55101";
            if (StateOfCurrentCall == CallStatePCL.StreamsRunning)
            {
                LinphoneManager.TerminateAllCalls();
            }
            else if(StateOfCurrentCall == CallStatePCL.IncomingReceived || StateOfCurrentCall == CallStatePCL.IncomingEarlyMedia)
            {
                LinphoneManager.AcceptCall();
                mockButton.IsVisible = false;
            }
            else if(call.Text == "Start Call")
            {
                if (!MockUser)
                {
                    toCall = address.Text;
                }
                LinphoneManager.CallSip(toCall);
            }
        }

        private void OnMockUserClicked(object sender, EventArgs e)
        {
            MockUser = true;
        }



    }
}

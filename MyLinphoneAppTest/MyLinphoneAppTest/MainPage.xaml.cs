using LibLinphone.Interfaces;
using LibLinphone.Views;
using MyLinphoneAppTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MyLinphoneAppTes
{
	public partial class MainPage : ContentPage, ILinphoneListener
	{
        private ILinphoneManager LinphoneManager = App.LinphoneManager;
        private bool MockUser;
        private CallPCL CurrentCall;
        private CallStatePCL StateOfCurrentCall;
        private bool isOutgoingCall = false;
        //Kalpa
        public MainPage()
		{
			InitializeComponent();
            
            LinphoneManager.AddLinphoneListenner(this);
            NavigationPage.SetHasNavigationBar(this, false);
        }

        public void OnCall(CallArgs e)
        {
            StateOfCurrentCall = e.CallState;
            CurrentCall = e.LCall;
            Debug.WriteLine("Call state changed: " + StateOfCurrentCall);

            call_status.Text = "Call state changed: " + StateOfCurrentCall;

            if (StateOfCurrentCall == CallStatePCL.IncomingReceived)
            {
                var page = new OnCallPage();
                page.RefuseCall += (s, args) =>
                {
                    LinphoneManager.TerminateAllCalls();
                };

                page.AcceptCall += (s, args) =>
                {
                    LinphoneManager.AcceptCall();
                };

                page.SetVideoView(CurrentCall, LinphoneManager);

                Navigation.PushAsync(page);

            }
            else if(StateOfCurrentCall == CallStatePCL.IncomingEarlyMedia)
            {
                
            }
            else if(StateOfCurrentCall == CallStatePCL.StreamsRunning)
            {
                if (isOutgoingCall)
                {
                    call.Text = "Terminate";
                    if (!CurrentCall.IsVideoEnabled)
                    {
                        LinphoneManager.SetViewCallOutgoing(CurrentCall);
                    }
                }
            }else if (StateOfCurrentCall == CallStatePCL.Error || StateOfCurrentCall == CallStatePCL.End)
            {
                call.Text = "Start Call";
               
                var stack = Navigation.NavigationStack; // remove on call page
                if(stack != null && stack.Count == 2)
                {
                    Navigation.PopAsync();
                }
                if (isOutgoingCall)
                {
                    isOutgoingCall = false;
                    contentViewVideo.IsVisible = false;
                    contentViewVideo.Content = null;

                }
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
                
            }
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            var domain = "f8dc7a13b724.f8dc7a13b7241518221438.ipvdesdev.vimar.cloud";
            var pwd = "WyRYnzbUp7dZW6dXfdFBhbJ5ttDC5zXs";
            var usr = "60001";
            string imei = "imei";
            string myName = "myName";
            string serverAddr = "192.168.1.5";
            string routeAddr = "192.168.1.5";
           
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
            string toCall = "800099";
            if (isOutgoingCall)
            {
                LinphoneManager.TerminateAllCalls();
            }
            else
            {
                contentViewVideo.IsVisible = true;
                contentViewVideo.Content = new LinphoneVideoView();
                if (!String.IsNullOrEmpty(address.Text))
                    toCall = address.Text;
                LinphoneManager.CallSip(toCall);
                isOutgoingCall = true;
            }
        }
        private Task taskRun = null;

        private void OnStartTestClicked(object sender, EventArgs e)
        {

            string toCall = "55101";
            //  string toCall = "800099";
            if (call.IsEnabled)
            {
                TestButton.Text = "Stop";
                call.IsEnabled = false;
                taskRun = new Task(async () =>
                {
                    while (true)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            contentViewVideo.IsVisible = true;
                            contentViewVideo.Content = new LinphoneVideoView();
                            LinphoneManager.CallSip(toCall);
                            isOutgoingCall = true;
                        });
                        await Task.Delay(TimeSpan.FromSeconds(8));
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            LinphoneManager.TerminateAllCalls();
                        });
                        await Task.Delay(TimeSpan.FromSeconds(4));
                    }
                    
                });

                taskRun.Start();
            }
            else
            {
                if(taskRun != null)
                {
                    taskRun.Dispose();
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        call.IsEnabled = true;
                        TestButton.Text = "Start test";
                    });
              
                }
            }

        }



        private void OnMockUserClicked(object sender, EventArgs e)
        {
            MockUser = true;
        }

        public void OnError(ErrorTypes type)
        {
            //throw new NotImplementedException();
        }
    }
}

using LibLinphone.Android.LinphoneUtils;
using LibLinphone.Droid.LinphoneUtils;
using LibLinphone.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(LinphoneManagerAndroid))]
namespace LibLinphone.Droid.LinphoneUtils
{

    public class LinphoneManagerAndroid : ILinphoneManager
    {
        LinphoneEngineAndroid LinphoneEngine;
        

        public bool AcceptCall()
        {
            return LinphoneEngine.AcceptCall();
        }

        public LinphoneManagerAndroid()
        {
            LinphoneEngine = LinphoneEngineAndroid.Instance;
        }

        public void AddLinphoneListenner(ILinphoneListener linphneListenner)
        {
            LinphoneEngine.AddLinphoneListenner(linphneListenner);
        }

        public void CallSip(string username)
        {
            LinphoneEngine.CallSip(username);
        }

        public int CallsNb()
        {
            return LinphoneEngine.CallsNb();
        }

        public void ChangeMicValue()
        {
            LinphoneEngine.ChangeMicValue();
        }

        public bool IsInCall()
        {
            return LinphoneEngine.IsInCall();
        }

        public void RegisterLinphone(string username, string password, string domain, string imei, string myName, string serverAddr, string routeAddr, bool isMock, bool isCloud)
        {
            LinphoneEngine.RegisterLinphone(username, password, domain, imei, myName, serverAddr, routeAddr, isMock, isCloud);
        }

        public void RemoveLinphoneListenner(ILinphoneListener linphneListenner)
        {
            LinphoneEngine.RemoveLinphoneListenner(linphneListenner);
        }
        
        public void RemoveAllListeners()
        {
            LinphoneEngine.RemoveAllLinphoneListener();
        }

        public void SetMicValue(bool value)
        {
            LinphoneEngine.SetMicValue(value);
        }

        public void SetViewCall(CallPCL call)
        {
            LinphoneEngine.SetViewCall(call);
        }

        public void SetViewCallOutgoing(CallPCL call)
        {
            LinphoneEngine.SetViewCallOutgoing(call);
        }

        public void TerminateAllCalls()
        {
            LinphoneEngine.TerminateAllCalls();
        }

        public void UnRegister()
        {
            LinphoneEngine.UnRegister();
        }
    }
}
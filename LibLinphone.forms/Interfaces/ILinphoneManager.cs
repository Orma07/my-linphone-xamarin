using System;

namespace LibLinphone.forms.Interfaces
{
    public interface ILinphoneManager
    {
        void SetViewCall(CallPCL call);
        void AddLinphoneListenner(ILinphoneListener linphneListenner);
        void RemoveLinphoneListenner(ILinphoneListener linphneListenner);
        void RemoveAllListeners();
        /// <summary>
        /// 
        /// </summary>
        /// <returns>tells me if call is realy accepted</returns>
        void AcceptCall();
        void TerminateAllCalls();
        void RegisterLinphone(
            string username,
            string password,
            string domain,
            string imei,
            string myName,
            string serverAddr,
            string routeAddr,
            bool isMock,
            bool isCloud);
        void UnRegister();
        void ChangeMicValue();
        void SetMicValue(bool value);
        int CallsNb();
        bool IsInCall();
        void CallSip(string username);
        void Set2FGains();
        void SetIpGains();
    }

    public class CallArgs : EventArgs
    {
        public CallPCL LCall { get; set; }
        public CallStatePCL CallState { get; set; }
        public string Message { get; set; }

        public CallArgs(CallPCL lcall, int state, string message, bool isVideoEnabled)
        {
            LCall = lcall;
            CallState = (CallStatePCL)state;
            Message = message;
            LCall.IsVideoEnabled = isVideoEnabled;
        }
    }

    public class CallPCL
    {
        public string UsernameCaller { get; set; }
        public bool IsVideoEnabled { get; set; }
    }

    public enum CallStatePCL
    {
        /// <summary>
        /// Initial state. 
        /// </summary>
        Idle = 0,
        /// <summary>
        /// Incoming call received. 
        /// </summary>
        IncomingReceived = 1,
        /// <summary>
        /// Outgoing call initialized. 
        /// </summary>
        OutgoingInit = 2,
        /// <summary>
        /// Outgoing call in progress. 
        /// </summary>
        OutgoingProgress = 3,
        /// <summary>
        /// Outgoing call ringing. 
        /// </summary>
        OutgoingRinging = 4,
        /// <summary>
        /// Outgoing call early media. 
        /// </summary>
        OutgoingEarlyMedia = 5,
        /// <summary>
        /// Connected. 
        /// </summary>
        Connected = 6,
        /// <summary>
        /// Streams running. 
        /// </summary>
        StreamsRunning = 7,
        /// <summary>
        /// Pausing. 
        /// </summary>
        Pausing = 8,
        /// <summary>
        /// Paused. 
        /// </summary>
        Paused = 9,
        /// <summary>
        /// Resuming. 
        /// </summary>
        Resuming = 10,
        /// <summary>
        /// Referred. 
        /// </summary>
        Referred = 11,
        /// <summary>
        /// Error. 
        /// </summary>
        Error = 12,
        /// <summary>
        /// Call end. 
        /// </summary>
        End = 13,
        /// <summary>
        /// Paused by remote. 
        /// </summary>
        PausedByRemote = 14,
        /// <summary>
        /// The call's parameters are updated for example when video is asked by remote. 
        /// </summary>
        UpdatedByRemote = 15,
        /// <summary>
        /// We are proposing early media to an incoming call. 
        /// </summary>
        IncomingEarlyMedia = 16,
        /// <summary>
        /// We have initiated a call update. 
        /// </summary>
        Updating = 17,
        /// <summary>
        /// The call object is now released. 
        /// </summary>
        Released = 18,
        /// <summary>
        /// The call is updated by remote while not yet answered (SIP UPDATE in early
        /// dialog received) 
        /// </summary>
        EarlyUpdatedByRemote = 19,
        /// <summary>
        /// We are updating the call while not yet answered (SIP UPDATE in early dialog
        /// sent) 
        /// </summary>
        EarlyUpdating = 20,
    }
}

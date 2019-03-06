using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Runtime;
using LibLinphone.forms.Interfaces;
using Linphone;
using Xamarin.Forms;
using Application = Android.App.Application;

#pragma warning disable 618

namespace LibLinphone.Android.LinphoneUtils
{
    public class LinphoneEngineAndroid
    {
        public static LinphoneEngineAndroid Instance => _instance ?? (_instance = new LinphoneEngineAndroid());
        
        private Core linphoneCore;
        public Core LinphoneCore => linphoneCore;
        public List<ILinphoneListener> LinphoneListeners { get; set; }
        public static string RcPath { get; private set; }
        public static string CaPath { get; private set; }
        public static string FactoryPath { get; private set; }
        public RegistrationState RegisterState { get; private set; }
        private string Imei;
        private string MyName;

        
        private static LinphoneEngineAndroid _instance;
        private const int PermissionsRequest = 101;        
        private Call LastCall;
        private int nRetryTerminateCalls;
        private bool iterateLinphoneCore = true;
        private CoreListener CoreListener;

        public bool EnableSpeaker { get; set; }

        private LinphoneEngineAndroid()
        {
            EnableSpeaker = true;

            

            Log("C# WRAPPER=" + LinphoneWrapper.VERSION);
            Log($"Linphone version {Core.Version}");

            CoreListener = Factory.Instance.CreateCoreListener();
            CoreListener.OnGlobalStateChanged = OnGlobal;
            LinphoneListeners = new List<ILinphoneListener>();
            RegisterState = RegistrationState.None;

            InitLinphoneCore();
            // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
           
            LogCodecs();

            CoreListener.OnCallStateChanged = OnCall;
            CoreListener.OnCallStatsUpdated = OnStats;
            CoreListener.OnRegistrationStateChanged = OnRegistration;

            CoreListener.OnConfiguringStatus = OnConfigurationStatus;

          

            LinphoneCoreIterate();
        }
        

        private void InitLinphoneCore()
        {
            linphoneCore = Factory.Instance.CreateCore(CoreListener, RcPath, FactoryPath, IntPtr.Zero, LinphoneAndroid.AndroidContext);

            // Required to be able to store logs as file
            //Core.SetLogCollectionPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            //Core.EnableLogCollection(LogCollectionState.Enabled);
            //CoreListener.OnLogCollectionUploadStateChanged = OnLogUpload;
            //UploadLogCommand();

#if DEBUGDEV
            // Required to be able to log Linphone Logs
            LoggingService.Instance.LogLevel = LogLevel.Debug;
            LinphoneWrapper.setNativeLogHandler();
            //LoggingService.Instance.Listener.OnLogMessageWritten = OnLog;
#endif

            linphoneCore.NetworkReachable = true;
            linphoneCore.RingDuringIncomingEarlyMedia = false;
            linphoneCore.VideoCaptureEnabled = false;
            linphoneCore.VideoDisplayEnabled = true;

            linphoneCore.RootCa = CaPath;
            linphoneCore.VerifyServerCertificates(true);

            linphoneCore.EchoCancellationEnabled = true;
            linphoneCore.EchoCancellerFilterName = "MSWebRTCAEC";

            //For MTS 4: beamforming_mic_dist_mm=74 beamforming_angle_deg=0 DON'T DELETE!
            //For MTS 7: beamforming_mic_dist_mm =184 beamforming_angle_deg=0 default value in linphonerc DON'T DELETE!

            // DON'T DELETE!
            // linphoneCore.BeamformingMicDist = 184f;
            // linphoneCore.BeamformingAngleDeg = 0;
            // linphoneCore.BeamformingEnabled = true;

        }

        public void SetGain(float play, float mic)
        {
            Log($"Setting gains: MIC = {mic}, PLAYBACK = {play}");
            linphoneCore.MicGainDb = mic;
            linphoneCore.PlaybackGainDb = play;
        }

        public void AcceptCall()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var call = linphoneCore.CurrentCall;
                if (call != null && (call.State == CallState.IncomingReceived || call.State == CallState.IncomingEarlyMedia))
                {
                    try
                    {
                        linphoneCore.AcceptCall(call);
                        var callParams = linphoneCore.CreateCallParams(call);
                        callParams.AddCustomHeader(LinphoneConstants.HEADER_MOBILE_IMEI, Imei);
                        callParams.AddCustomHeader(LinphoneConstants.HEADER_DEVICE_NAME, MyName);
                        callParams.VideoEnabled = true;
                        callParams.VideoDirection = MediaDirection.RecvOnly;
                        callParams.AudioDirection = MediaDirection.SendRecv;
                        linphoneCore.AcceptCallWithParams(call, callParams);
                    }
                    catch (Exception ex)
                    {
                        Utils.TraceException(ex);
                    }
                }
            });
        }

        public void ChangeMicValue()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                linphoneCore.MicEnabled = !linphoneCore.MicEnabled;                
            });
        }
        
        public void SetMicValue(bool value)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                linphoneCore.MicEnabled = value;
            });
        }

        public bool IsInCall()
        {
            var result = false;
            var state = linphoneCore.CurrentCall?.State;
            if (state != null)
                result = true;
            return result;
        }

        public int CallsNb()
        {
            return linphoneCore.CallsNb;
        }

        public void CallSip(string username)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (RegisterState == RegistrationState.Ok && linphoneCore.CallsNb == 0)
                    {
                        var addr = linphoneCore.InterpretUrl(username);
                        if (addr != null)
                        {
                            var callParams = linphoneCore.CreateCallParams(null);
                            callParams.AddCustomHeader(LinphoneConstants.HEADER_MOBILE_IMEI, Imei);
                            callParams.AddCustomHeader(LinphoneConstants.HEADER_DEVICE_NAME, MyName);
                            callParams.VideoEnabled = true;
                            callParams.VideoDirection = MediaDirection.RecvOnly;
                            callParams.AudioDirection = MediaDirection.SendRecv;
                            linphoneCore.InviteAddressWithParams(addr, callParams);
                            //linphoneCore.InviteAddress(addr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"CallSip()_Android - {username} is invalid username");
                    Utils.TraceException(ex);
                }
            });
        }

        public void SetViewCall(CallPCL call)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (linphoneCore.CallsNb > 0)
                {
                    var currentCall = linphoneCore.Calls.FirstOrDefault(lcall => lcall.RemoteAddress.Username == call.UsernameCaller);
                    if (currentCall != null)
                    {
                        linphoneCore.VideoDisplayEnabled = true;
                        linphoneCore.VideoAdaptiveJittcompEnabled = true;

                        var param = linphoneCore.CreateCallParams(currentCall);
                        param.VideoEnabled = true;
                        param.VideoDirection = MediaDirection.RecvOnly;
                        param.AudioDirection = MediaDirection.SendRecv;

                        currentCall.AcceptEarlyMediaWithParams(param);
                    }
                    else
                        Log($"SetViewCall()_Android, call from: {call.UsernameCaller} is not call in linphoneCore");
                }
            });
        }

        public void SetViewCallOutgoing(CallPCL call)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (linphoneCore.CallsNb > 0)
                {
                    var currentCall = linphoneCore.Calls.FirstOrDefault(lcall => lcall.RemoteAddress.Username == call.UsernameCaller);
                    if (currentCall != null)
                    {
                        linphoneCore.VideoDisplayEnabled = true;
                        linphoneCore.VideoAdaptiveJittcompEnabled = true;

                        var param = linphoneCore.CreateCallParams(currentCall);
                        param.VideoEnabled = true;
                        param.VideoDirection = MediaDirection.RecvOnly;
                        param.AudioDirection = MediaDirection.SendRecv;
                        linphoneCore.UpdateCall(currentCall, param);
                    }
                    else
                        Log($"SetViewCallOutgoing()_Android, call from: {call.UsernameCaller} is not call in linphoneCore");
                }
            });
        }

        public void TerminateAllCalls()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (linphoneCore.CurrentCall != null)
                        linphoneCore.TerminateAllCalls();
                    else
                    {
                        try
                        {
                            linphoneCore.TerminateCall(LastCall);
                        }
                        catch (Exception ex)
                        {
                            Utils.TraceException(ex);
                        }
                    }

                    nRetryTerminateCalls = 0;
                }
                catch (Exception e)
                {
                    Utils.TraceException(e);

                    nRetryTerminateCalls++;
                    if (nRetryTerminateCalls < 3)
                        TerminateAllCalls();
                    else
                        Log("terminate call failed after 3 attempts");
                }
            });
        }

        #region Platform method
        public static void Start(string caName=null)
        {
            Java.Lang.JavaSystem.LoadLibrary("c++_shared");
            Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
            Java.Lang.JavaSystem.LoadLibrary("ortp");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer");
            Java.Lang.JavaSystem.LoadLibrary("linphone");

            // This is mandatory for Android
            LinphoneAndroid.setAndroidContext(JNIEnv.Handle, Application.Context.Handle);

            var assets = Application.Context.Assets;
            var path = Application.Context.FilesDir.AbsolutePath;
            
            var rcPath = path + "/default_rc";
            using (var br = new BinaryReader(Application.Context.Assets.Open("linphonerc_default")))
            {
                using (var bw = new BinaryWriter(new FileStream(rcPath, FileMode.Create)))
                {
                    var buffer = new byte[2048];
                    int length;
                    while ((length = br.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, length);
                    }
                }     
            }
            RcPath = rcPath;
            
            if (!string.IsNullOrEmpty(caName))
            {
                var caPath = path + $"/{caName}";
                CaPath = caPath;
                using (var br = new BinaryReader(Application.Context.Assets.Open(caName)))
                {
                    using (var bw = new BinaryWriter(new FileStream(caPath, FileMode.Create)))
                    {
                        var buffer = new byte[2048];
                        int length;
                        while ((length = br.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            bw.Write(buffer, 0, length);
                        }
                    }
                }
            }

            var factoryPath = path + "/factory_rc";
            if (!File.Exists(factoryPath))
            {
                using (var sr = new StreamReader(assets.Open("linphonerc_factory")))
                {
                    var content = sr.ReadToEnd();
                    File.WriteAllText(factoryPath, content);
                }
            }
            FactoryPath = factoryPath;

            if (int.Parse(global::Android.OS.Build.VERSION.Sdk) >= 23)
            {
                var permissions = new List<string>();
                if (Application.Context.CheckSelfPermission(Manifest.Permission.RecordAudio) != Permission.Granted)
                    permissions.Add(Manifest.Permission.RecordAudio);

                if (permissions.Count > 0)
                {
                    var currentActivity = (Activity)Forms.Context;
                    currentActivity.RequestPermissions(permissions.ToArray(), PermissionsRequest);
                }
            }
        }
        
        private void OnConfigurationStatus(Core lc, ConfiguringState status, string message)
        {
            Log($"OnConfiguration, status: {status}");
        }

        private void OnStats(Core lc, Call call, CallStats stats)
        {
            Log("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");
        }

        private void SaveLastCall(Call lcall, CallState state)
        {
            if (state == CallState.End || state == CallState.Error)
                LastCall = null;

            if ((state == CallState.IncomingReceived || state == CallState.OutgoingInit) && LastCall != null)
            {
                linphoneCore.TerminateCall(LastCall);
                LastCall = null;
            }

            if ((state == CallState.IncomingReceived || state == CallState.OutgoingInit) && LastCall == null)
                LastCall = lcall;
        }

        private void OnCall(Core lc, Call lcall, CallState state, string message)
        {
            try
            {
                if(state == CallState.IncomingReceived || state == CallState.OutgoingRinging)
                {
                    //linphoneCore.StartEchoCancellerCalibration();
                    if (EnableSpeaker)
                        EnableAndroidSpeaker();
                }

                SaveLastCall(lcall, state);
                

                var call = new CallPCL {UsernameCaller = lcall.RemoteAddress.Username};
                var param = linphoneCore.CreateCallParams(lcall);

                lock (LinphoneListeners)
                {
                    for (var i = 0; i < LinphoneListeners.Count; i++)
                    {
                        try
                        {
                            var listener = LinphoneListeners[i];
                            listener.OnCall(new CallArgs(call, (int) state, message, param.VideoEnabled));
                        }
                        catch (Exception ex)
                        {
                            Utils.TraceException(ex);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.TraceException(e);
            }

        }

        private void EnableAndroidSpeaker()
        {
            var audioManager = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            audioManager.Mode = Mode.InCall;
            if (!audioManager.SpeakerphoneOn)
            {
                audioManager.SpeakerphoneOn = true;
            }
                
        }

        /// <summary>
        /// To test codecs
        /// </summary>
        private void LogCodecs()
        {
            try
            {
                var videoCodecs = linphoneCore.VideoPayloadTypes;
                var audioCodecs = linphoneCore.AudioPayloadTypes;
                
                foreach (var pt in videoCodecs)
                {
                    if (!pt.Enabled())
                        pt.Enable(true);

                    Log($"VIDEO - Payload: {pt.MimeType}/{pt.ClockRate}/{pt.NormalBitrate}, Enabled: {pt.Enabled().ToString()}, IsUsable {pt.IsUsable}");
                }
                
                foreach (var pt in audioCodecs)
                {
                    if (!pt.Enabled())
                        pt.Enable(true);

                    Log($"AUDIO - Payload: {pt.MimeType}/{pt.ClockRate}/{pt.NormalBitrate}, Enabled: {pt.Enabled().ToString()}, IsUsable {pt.IsUsable}");
                }
            }
            catch (Exception ex)
            {
                Utils.TraceException(ex);
            }
        }

        private void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Log("LINPHONE - Global state changed -> " + gstate);
        }

        private static void Log(string message,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if(lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
            {
                var className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
                message = $"[{className}.{callingMethod}():{callingFileLineNumber}] - D - {message}";
            }

            Debug.WriteLine(message);
        }

        public void LinphoneCoreIterate()
        {
            iterateLinphoneCore = true;
            Device.StartTimer(TimeSpan.FromMilliseconds(50), () =>
            {
                try
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        linphoneCore.Iterate();
                    });
                }
                catch(Exception e)
                {
                    Utils.TraceException(e);
                    for (var i = 0; i < LinphoneListeners.Count; i++)
                    {
                        try
                        {
                            var listener = LinphoneListeners[i];
                            listener.OnError(ErrorTypes.CoreIterateFailed);
                        }
                        catch (Exception ex)
                        {
                            Utils.TraceException(ex);
                        }
                    }
                }
                return iterateLinphoneCore;
            });
        }

        private void OnRegistration(Core lc, ProxyConfig config, RegistrationState state, string message)
        {
            if (config?.FindAuthInfo() != null)
            {
                Log($"Register, state - {state}, Username - {config.FindAuthInfo().Username}, " +
                    $"domain - {config.Domain}, route - {config.Route}, message - {message}");
            }
            else
            {
                Log($"Register, state - {state}, message - {message}");
               // Init();
            }
            
            RegisterState = state;
            lock (LinphoneListeners)
            {
                if (LinphoneListeners != null)
                {
                    for (var i = 0; i < LinphoneListeners.Count; i++)
                    {
                        try
                        {
                            var listener = LinphoneListeners[i];
                            listener.OnRegistration((RegistrationStatePCL)state, message);
                        }
                        catch (Exception ex)
                        {
                            Utils.TraceException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Register to contact center
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="domain"></param>
        /// <param name="imei"></param>
        /// <param name="myName"></param>
        /// <param name="serverAddr"></param>
        /// <param name="routeAddr"></param>
        /// <param name="isMock"></param>
        /// <param name="isCloud"></param>
        public void RegisterLinphone(
            string username,
            string password,
            string domain,
            string imei,
            string myName,
            string serverAddr,
            string routeAddr,
            bool isMock,
            bool isCloud)
        {

            Device.BeginInvokeOnMainThread(() =>
            {
                Imei = imei;
                MyName = myName;
                try
                {
                    linphoneCore.ClearAllAuthInfo();
                    linphoneCore.ClearProxyConfig();
                    var authInfo = Factory.Instance.CreateAuthInfo(username, null, password, null, null, domain);
                    linphoneCore.AddAuthInfo(authInfo);         
                    
                    var identity = Factory.Instance.CreateAddress($"sip:{username}@{domain}");
                    var proxyConfig = linphoneCore.CreateProxyConfig();
                    proxyConfig.Edit();
                        
                    if (isMock)
                        identity = Factory.Instance.CreateAddress($"sip:sample@domain.tld");
                 
                    if (isCloud)
                    {
                        identity.Transport = TransportType.Tls;
                        var transport = linphoneCore.Transports;
                        transport.TcpPort = 0;
                        transport.TlsPort = -1;
                        transport.UdpPort = 0;
                        linphoneCore.Transports = transport;
                        proxyConfig.ServerAddr =$"<sip:{serverAddr};transport=tls>";
                        proxyConfig.Route = $"<sip:{serverAddr};transport=tls>";
                    }
                    else
                    {
                        identity.Transport = TransportType.Tcp;
                        var transport = linphoneCore.Transports;
                        transport.TcpPort = -1;
                        transport.TlsPort = 0;
                        transport.UdpPort = 0;
                        linphoneCore.Transports = transport;
                        proxyConfig.ServerAddr = $"<sip:{serverAddr};transport=tcp>";
                        proxyConfig.Route = $"<sip:{serverAddr};transport=tcp>";
                        proxyConfig.Expires = LinphoneConstants.LINPHONE_PROXY_CFG_EXPIRE_TIME_SEC;
                        proxyConfig.PublishEnabled = false;
                        proxyConfig.ContactParameters = "+sip.instance=\"<urn:uuid:" + imei + ">\"";
                    }
                    
                    Log($"Transports, TCP: {linphoneCore.Transports.TcpPort}, TLS: {linphoneCore.Transports.TlsPort}, UDP: {linphoneCore.Transports.UdpPort}");
                    identity.Username = username;
                    identity.Domain = domain;
                    //identity.Password = password;
                   
                    if (!isMock)
                    {
                        proxyConfig.SetCustomHeader(LinphoneConstants.HEADER_MOBILE_IMEI, imei);
                        proxyConfig.SetCustomHeader(LinphoneConstants.HEADER_DEVICE_NAME, myName);
                    }
    
                    proxyConfig.IdentityAddress = identity;
                   
                    proxyConfig.RegisterEnabled = true;
                    proxyConfig.Done();
                    linphoneCore.AddProxyConfig(proxyConfig);
                    linphoneCore.DefaultProxyConfig = proxyConfig;
    
                    linphoneCore.RefreshRegisters();
                }
                catch (Exception ex)
                {
                    Utils.TraceException(ex);
                }
            });
        }

        public void UnRegister()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    foreach (var proxyCfg in linphoneCore.ProxyConfigList)
                    {

                        Log($"Unregistering {proxyCfg.IdentityAddress}");
                        proxyCfg.Edit();
                        proxyCfg.RegisterEnabled = false;
                        proxyCfg.Done();

                    }

                    linphoneCore.ClearAllAuthInfo();
                    linphoneCore.ClearProxyConfig();
                    InitLinphoneCore();
                }
                catch (Exception ex)
                {
                    Utils.TraceException(ex);
                }
            });
        }

        public void AddLinphoneListenner(ILinphoneListener linphneListenner)
        {
            try
            {
                lock (LinphoneListeners)
                {
                    LinphoneListeners.Add(linphneListenner);
                }
            }
            catch (Exception ex)
            {
                Utils.TraceException(ex);
            }
        }

        public void RemoveLinphoneListenner(ILinphoneListener linphneListenner)
        {
            try
            {
                lock (LinphoneListeners)
                {
                    LinphoneListeners.Remove(linphneListenner);
                }
            }
            catch (Exception ex)
            {
                Utils.TraceException(ex);
            }
        }
        
        public void RemoveAllLinphoneListener()
        {
            try
            {
                lock (LinphoneListeners)
                {
                    LinphoneListeners.Clear();
                }
            }
            catch (Exception ex)
            {
                Utils.TraceException(ex);
            }
        }
        #endregion
    }
}
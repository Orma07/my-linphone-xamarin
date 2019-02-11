using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using LibLinphone.forms.Interfaces;
using Linphone;
#pragma warning disable 618

namespace LibLinphone.Android.LinphoneUtils
{
    public class LinphoneEngineAndroid
    {
        public static LinphoneEngineAndroid Instance => _instance ?? (_instance = new LinphoneEngineAndroid());
        public Core LinphoneCore { get; }
        public List<ILinphoneListener> LinphoneListeners { get; set; }
        public static string RcPath { get; private set; }
        public static string CaPath { get; private set; }
        public static string FactoryPath { get; private set; }
        public RegistrationState RegisterState { get; private set; }

        //private static readonly CancellationTokenSource CancelLogTask = new CancellationTokenSource();
        
        private static LinphoneEngineAndroid _instance;
        private const int PermissionsRequest = 101;        
        private Call LastCall;
        private int nRetryTerminateCalls;

        private LinphoneEngineAndroid()
        {
            Log("C# WRAPPER=" + LinphoneWrapper.VERSION);
            Log($"Linphone version {Core.Version}");
            
            var coreListener = Factory.Instance.CreateCoreListener();
            coreListener.OnGlobalStateChanged = OnGlobal;
            LinphoneListeners = new List<ILinphoneListener>();
            RegisterState = RegistrationState.None;

            // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
            LinphoneCore = Factory.Instance.CreateCore(coreListener, RcPath, FactoryPath, IntPtr.Zero, LinphoneAndroid.AndroidContext);

            // Required to be able to store logs as file
            //Core.SetLogCollectionPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            //Core.EnableLogCollection(LogCollectionState.Enabled);

            //UploadLogCommand();
            //LoggingService.Instance.LogLevel = LogLevel.Debug;
            //LinphoneWrapper.setNativeLogHandler();
            //LoggingService.Instance.Listener.OnLogMessageWritten = OnLog;
            //CoreListener.OnLogCollectionUploadStateChanged = OnLogUpload;
            
            LinphoneCore.NetworkReachable = true;
            LinphoneCore.RingDuringIncomingEarlyMedia = false;
            LinphoneCore.VideoCaptureEnabled = false;
            LinphoneCore.VideoDisplayEnabled = true;
            LinphoneCore.RootCa = CaPath;
            LinphoneCore.VerifyServerCertificates(true);

            Log("Transports, " +
                $"TCP: {LinphoneCore.Transports.TcpPort}, " +
                $"TLS: {LinphoneCore.Transports.TlsPort}, " +
                $"UDP: {LinphoneCore.Transports.UdpPort}");

            LogCodecs();

            coreListener.OnCallStateChanged = OnCall;
            coreListener.OnCallStatsUpdated = OnStats;
            coreListener.OnRegistrationStateChanged = OnRegistration;

            coreListener.OnConfiguringStatus = OnConfigurationStatus;

            LinphoneCore.EchoCancellationEnabled = true;
            LinphoneCore.EchoCancellerFilterName = "MSWebRTCAEC";

            //For MTS 4: beamforming_mic_dist_mm=74 beamforming_angle_deg=0 DON'T DELETE!
            //For MTS 7: beamforming_mic_dist_mm =184 beamforming_angle_deg=0 default value in linphonerc DON'T DELETE!

            // DON'T DELETE!
            // linphoneCore.BeamformingMicDist = 184f;
            // linphoneCore.BeamformingAngleDeg = 0;
            // linphoneCore.BeamformingEnabled = true;

            LinphoneCoreIterateAsync();
        }

        public bool AcceptCall()
        {
            var result = false;
            var call = LinphoneCore.CurrentCall;
            if (call != null && (call.State == CallState.IncomingReceived || call.State == CallState.IncomingEarlyMedia))
            {
                LinphoneCore.AcceptCall(call);
                result = true;
            }
            else
            {
                try
                {
                    LinphoneCore.AcceptCall(LastCall);
                }
                catch (Exception ex)
                {
                    Utils.TraceException(ex);
                }
            }
            return result;
        }

        public void ChangeMicValue()
        {
            LinphoneCore.MicEnabled = !LinphoneCore.MicEnabled;
        }
        
        public void SetMicValue(bool value)
        {
            LinphoneCore.MicEnabled = value;
        }

        public bool IsInCall()
        {
            var result = false;
            var state = LinphoneCore.CurrentCall?.State;
            if (state != null)
                result = true;
            return result;
        }

        public int CallsNb()
        {
            return LinphoneCore.CallsNb;
        }

        public void CallSip(string username)
        {
            try
            {
                if (RegisterState == RegistrationState.Ok && LinphoneCore.CallsNb == 0)
                {
                    var addr = LinphoneCore.InterpretUrl(username);
                    if (addr != null)
                        LinphoneCore.InviteAddress(addr);
                }
            }
            catch(Exception ex) 
            {
                Log($"CallSip()_Android - {username} is invalid username");
                Utils.TraceException(ex);
            }
        }

        public void SetViewCall(CallPCL call)
        {
            if (LinphoneCore.CallsNb > 0)
            {
                var currentCall = LinphoneCore.Calls.FirstOrDefault(lcall => lcall.RemoteAddress.Username == call.UsernameCaller);
                if (currentCall != null)
                {
                    LinphoneCore.VideoDisplayEnabled = true;
                    LinphoneCore.VideoAdaptiveJittcompEnabled = true;

                    var param = LinphoneCore.CreateCallParams(currentCall);
                    param.VideoEnabled = true;
                    param.VideoDirection = MediaDirection.RecvOnly;
                    param.AudioDirection = MediaDirection.SendRecv;
                  
                    currentCall.AcceptEarlyMediaWithParams(param);
                }
                else
                    Log($"SetViewCall()_Android, call from: {call.UsernameCaller} is not call in linphoneCore");
            }
        }

        public void SetViewCallOutgoing(CallPCL call)
        {
            if (LinphoneCore.CallsNb > 0)
            {
                var currentCall = LinphoneCore.Calls.FirstOrDefault(lcall => lcall.RemoteAddress.Username == call.UsernameCaller);
                if (currentCall != null)
                {
                    LinphoneCore.VideoDisplayEnabled = true;
                    LinphoneCore.VideoAdaptiveJittcompEnabled = true;
                    
                    var param = LinphoneCore.CreateCallParams(currentCall);
                    param.VideoEnabled = true;
                    param.VideoDirection = MediaDirection.RecvOnly;
                    param.AudioDirection = MediaDirection.SendRecv;
                    LinphoneCore.UpdateCall(currentCall, param);

                }
                else
                {
                    Log($"SetViewCallOutgoing()_Android, call from: {call.UsernameCaller} is not call in linphoneCore");
                }
            }
        }

        public void TerminateAllCalls()
        {
            try
            {
                if (LinphoneCore.CurrentCall != null)
                    LinphoneCore.TerminateAllCalls();
                else
                {
                    try
                    {
                        LinphoneCore.TerminateCall(LastCall);
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

            //ScheduleJob();
            //var startServiceIntent = new Intent(Application.Context, typeof(LinphoneJobService));
            //Application.Context.StartService(startServiceIntent);

            if (int.Parse(global::Android.OS.Build.VERSION.Sdk) >= 23)
            {
                var permissions = new List<string>();
                if (Application.Context.CheckSelfPermission(Manifest.Permission.RecordAudio) != Permission.Granted)
                    permissions.Add(Manifest.Permission.RecordAudio);

                if (permissions.Count > 0)
                {
                    var currentActivity = (Activity)Xamarin.Forms.Forms.Context;
                    currentActivity.RequestPermissions(permissions.ToArray(), PermissionsRequest);
                }
            }
        }

        /*private void UploadLogCommand()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    LinphoneCore.UploadLogCollection();
                    await Task.Delay(TimeSpan.FromSeconds(60), CancelLogTask.Token);
                }
            }, CancelLogTask.Token);
        }*/

        /*private void OnLogUpload(Core lc, CoreLogCollectionUploadState state, string info)
        {
            Log(state == CoreLogCollectionUploadState.Delivered
                ? $"linphone log upload, link -> {info}"
                : $"linphone log upload state -> {state}");
        }*/

        /*private void OnLog(LoggingService logService, string domain, LogLevel lev, string message)
        {
            var now = DateTime.Now.ToString("hh:mm:ss");
            var log = now + " [";
            switch (lev)
            {
                case LogLevel.Debug:
                    log += "DEBUG";
                    break;
                case LogLevel.Error:
                    log += "ERROR";
                    break;
                case LogLevel.Message:
                    log += "MESSAGE";
                    break;
                case LogLevel.Warning:
                    log += "WARNING";

                    break;
                case LogLevel.Fatal:
                    log += "FATAL";
                    break;
                case LogLevel.Trace:
                    log += "TRACE";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lev), lev, null);
            }
            log += "] (" + domain + ") " + message;
            Log(log);
        }*/

        private void OnConfigurationStatus(Core lc, ConfiguringState status, string message)
        {
            Log($"OnConfiguration, status: {status}");
        }

        private void OnStats(Core lc, Call call, CallStats stats)
        {
            Log("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");
        }

        private void OnCall(Core lc, Call lcall, CallState state, string message)
        {
            try
            {
                if (state == CallState.End || state == CallState.Error)
                    LastCall = null;

                if ((state == CallState.IncomingReceived || state == CallState.OutgoingInit) && LastCall != null)
                {
                    LinphoneCore.TerminateCall(LastCall);
                    LastCall = null;
                }

                if ((state == CallState.IncomingReceived || state == CallState.OutgoingInit) && LastCall == null)
                    LastCall = lcall;

                var call = new CallPCL {UsernameCaller = lcall.RemoteAddress.Username};
                var param = LinphoneCore.CreateCallParams(lcall);

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

        /// <summary>
        /// To test codecs
        /// </summary>
        private void LogCodecs()
        {
            try
            {
                var videoCodecs = LinphoneCore.VideoPayloadTypes;
                var audioCodecs = LinphoneCore.AudioPayloadTypes;
                
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

        private static void Log(string message)
        {
            Debug.WriteLine($"LINPHONE MANAGER: {message}");
        }

        
        
        public void LinphoneCoreIterateAsync()
        {
            Xamarin.Forms.Device.StartTimer(TimeSpan.FromMilliseconds(50), () =>
            {
                try
                {
                   // Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                   // {
                        LinphoneCore.Iterate();
                   // });
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
                return true;
            });
        }

        private void OnRegistration(Core lc, ProxyConfig config, RegistrationState state, string message)
        {
            Log($"Register, state - {state}, Username - {config.FindAuthInfo().Username}, domain - {config.Domain}, route - {config.Route}, message - {message}");
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

        public async void RegisterAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
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

            try
            {

                var prop = Xamarin.Forms.Application.Current.Properties;

                var authInfo = Factory.Instance.CreateAuthInfo(username, null, password, null, null, domain);
                LinphoneCore.AddAuthInfo(authInfo);         
                
                var identity = Factory.Instance.CreateAddress($"sip:{username}@{domain}");
                var proxyConfig = LinphoneCore.CreateProxyConfig();
                proxyConfig.Edit();
                if (isMock)
                {
                    identity = Factory.Instance.CreateAddress($"sip:sample@domain.tld");
                }
             
             
                if (isCloud)
                {
                    identity.Transport = TransportType.Tls;
                    var transport = LinphoneCore.Transports;
                    transport.TcpPort = 0;
                    transport.TlsPort = -1;
                    transport.UdpPort = 0;
                    LinphoneCore.Transports = transport;
                }
                else
                {
                    identity.Transport = TransportType.Tcp;
                    var transport = LinphoneCore.Transports;
                    transport.TcpPort = -1;
                    transport.TlsPort = 0;
                    transport.UdpPort = 0;
                    LinphoneCore.Transports = transport;
                }
                Log($"Transports, TCP: {LinphoneCore.Transports.TcpPort}, TLS: {LinphoneCore.Transports.TlsPort}, UDP: {LinphoneCore.Transports.UdpPort}");
                identity.Username = username;
                identity.Domain = domain;
                //identity.Password = password;

               
                if (!isMock)
                {
                    proxyConfig.SetCustomHeader("Mobile-IMEI", imei);
                    proxyConfig.SetCustomHeader("MyName", myName);
                }

                proxyConfig.IdentityAddress = identity;
                proxyConfig.ServerAddr = serverAddr;
                proxyConfig.Route = routeAddr;  //domain;
                proxyConfig.RegisterEnabled = true;
                proxyConfig.Done();
                LinphoneCore.AddProxyConfig(proxyConfig);
                LinphoneCore.DefaultProxyConfig = proxyConfig;

                LinphoneCore.RefreshRegisters();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

        }

        public void UnRegister()
        {
            try
            {
                LinphoneCore.ClearProxyConfig();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
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
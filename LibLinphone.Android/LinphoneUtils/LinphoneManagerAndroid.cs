using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using LibLinphone.Droid.LinphoneUtils;
using LibLinphone.Interfaces;
using Linphone;

[assembly: Xamarin.Forms.Dependency(typeof(LinphoneManagerAndroid))]
namespace LibLinphone.Droid.LinphoneUtils
{

    public class LinphoneManagerAndroid : ILinphoneManager
    {
        private static CoreListener CoreListener;
        public static List<ILinphneListenner> LinphoneListenners { get; set; }
        const int PERMISSIONS_REQUEST = 101;

        public static string RcPath { get; private set; }
        public static string CaPath { get; private set; }
        public static string FactoryPath { get; private set; }


        public bool AcceptCall()
        {
            bool result = false;
            var call = LinphoneCore.CurrentCall;
            if (call != null && (call.State == CallState.IncomingReceived || call.State == CallState.IncomingEarlyMedia))
            {
                LinphoneCore.AcceptCall(call);
                result = true;
            }
            return result;
        }

        public void ChangeMicValue()
        {
            linphoneCore.MicEnabled = !linphoneCore.MicEnabled;
        }
        public void SetMicValue(bool value)
        {
            linphoneCore.MicEnabled = value;
        }

        public bool IsInCall()
        {
            bool result = false;
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
            try
            {
                if (linphoneCore.CallsNb == 0)
                {
                    var addr = linphoneCore.InterpretUrl(username);
                    //addr.Transport = TransportType.Tcp;
                    linphoneCore.InviteAddress(addr);
                }
            }
            catch (Exception ex)
            {
                Log($"CallSip()_Android - {username} is invalid username");
            }
        }

        public void SetViewCall(CallPCL call)
        {

            if (linphoneCore.CallsNb > 0)
            {
                Call currentCall = linphoneCore.Calls.Where(lcall => lcall.RemoteAddress.Username == call.UsernameCaller).FirstOrDefault();
                if (currentCall != null)
                {
                    linphoneCore.VideoDisplayEnabled = true;
                    linphoneCore.VideoAdaptiveJittcompEnabled = true;
                    CallParams param = linphoneCore.CreateCallParams(currentCall);
                   
                    param.VideoEnabled = true;
                    param.VideoDirection = MediaDirection.RecvOnly;
                    param.AudioDirection = MediaDirection.SendRecv;
                    currentCall.AcceptEarlyMediaWithParams(param);
                }
                else
                {
                    Log($"SetViewCall()_Android, call from: {call.UsernameCaller} is not call in linphoneCore");
                }
                //lc.UpdateCall(call, param);

            }
        }

        public void SetViewCallOutgoing(CallPCL call)
        {
            if (linphoneCore.CallsNb > 0)
            {
                Call currentCall = linphoneCore.Calls.Where(lcall => lcall.RemoteAddress.Username == call.UsernameCaller).FirstOrDefault();
                if (currentCall != null)
                {
                    linphoneCore.VideoDisplayEnabled = true;
                    linphoneCore.VideoAdaptiveJittcompEnabled = true;
                    CallParams param = linphoneCore.CreateCallParams(currentCall);
                    param.VideoEnabled = true;
                    param.VideoDirection = MediaDirection.RecvOnly;
                    param.AudioDirection = MediaDirection.SendRecv;
                    linphoneCore.UpdateCall(currentCall, param);
                    
                }
                else
                {
                    Log($"SetViewCallOutgoing()_Android, call from: {call.UsernameCaller} is not call in linphoneCore");
                }
                //lc.UpdateCall(call, param);

            }
        }

        public void TerminateAllCalls()
        {
            try
            {
                if (linphoneCore.CurrentCall != null)
                    linphoneCore.TerminateAllCalls();
            }
            catch(Exception ex)
            {
                Log("terminate call failed");
            }
        }


        public LinphoneManagerAndroid()
        {
            InitLinphone();
        }








        #region Platform method
        private static Core linphoneCore;
        public static Core LinphoneCore
        {
            get
            {
                return linphoneCore;
            }
        }

        
        public static void Start()
        {
            Java.Lang.JavaSystem.LoadLibrary("c++_shared");
            Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
            Java.Lang.JavaSystem.LoadLibrary("ortp");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_base");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer_voip");
            Java.Lang.JavaSystem.LoadLibrary("linphone");

            // This is mandatory for Android
            LinphoneAndroid.setAndroidContext(JNIEnv.Handle, Application.Context.Handle);

            AssetManager assets = Application.Context.Assets;
            string path = Application.Context.FilesDir.AbsolutePath;
            string rc_path = path + "/default_rc";
            string ca_path = path + "/rootca.pem";
            using (var br = new BinaryReader(Application.Context.Assets.Open("linphonerc_default")))
            {
                using (var bw = new BinaryWriter(new FileStream(rc_path, FileMode.Create)))
                {
                    byte[] buffer = new byte[2048];
                    int length = 0;
                    while ((length = br.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, length);
                    }
                }

                RcPath = rc_path;
                CaPath = ca_path;
            }

            string factory_path = path + "/factory_rc";
            if (!File.Exists(factory_path))
            {
                using (StreamReader sr = new StreamReader(assets.Open("linphonerc_factory")))
                {
                    string content = sr.ReadToEnd();
                    File.WriteAllText(factory_path, content);
                }
            }

            FactoryPath = factory_path;
            //ScheduleJob();
            //var startServiceIntent = new Intent(Application.Context, typeof(LinphoneJobService));
            //Application.Context.StartService(startServiceIntent);

            if (Int32.Parse(global::Android.OS.Build.VERSION.Sdk) >= 23)
            {
                List<string> Permissions = new List<string>();
                if (Application.Context.CheckSelfPermission(Manifest.Permission.RecordAudio) != Permission.Granted)
                {
                    Permissions.Add(Manifest.Permission.RecordAudio);
                }
                if (Permissions.Count > 0)
                {
                    var currentActivity = (Activity)Xamarin.Forms.Forms.Context;
                    currentActivity.RequestPermissions(Permissions.ToArray(), PERMISSIONS_REQUEST);
                }

            }

            InitLinphone();
            LinphoneCoreIterateAsync();
        }



        public static void InitLinphone()
        {
            if (linphoneCore == null)
            {
                Log("C# WRAPPER=" + LinphoneWrapper.VERSION);
                CoreListener listener = Factory.Instance.CreateCoreListener();
                LinphoneListenners = new List<ILinphneListenner>();


                // Giving app context in CreateCore is mandatory for Android to be able to load grammars (and other assets) from AAR
                linphoneCore = Factory.Instance.CreateCore(listener, RcPath, FactoryPath, IntPtr.Zero, LinphoneAndroid.AndroidContext);
                // Required to be able to store logs as file
                Core.SetLogCollectionPath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData));

                LoggingService.Instance.LogLevel = LogLevel.Debug;
                LoggingService.Instance.Listener.OnLogMessageWritten = OnLog;

                listener.OnGlobalStateChanged = OnGlobal;
                //linphoneCore.rin
                linphoneCore.NetworkReachable = true;

                linphoneCore.RingDuringIncomingEarlyMedia = true;

                //LinphoneCore.RootCa = DeviceInfoMobile.CaPath;

                //Log(linphoneCore.roo)

                linphoneCore.VideoCaptureEnabled = false;
                linphoneCore.VideoDisplayEnabled = true;

                linphoneCore.Transports.TcpPort = -1;
                linphoneCore.Transports.TlsPort = 0;
                linphoneCore.Transports.UdpPort = 0;



                //linphoneCore.PrintMyInt(7, 13);

                Log($"Transports, TCP: {linphoneCore.Transports.TcpPort}, TLS: {linphoneCore.Transports.TlsPort}, UDP: {linphoneCore.Transports.UdpPort}");
                Log($"used transports is {linphoneCore.TransportsUsed}");

                LogCodecs();

                CoreListener = Factory.Instance.CreateCoreListener();
                CoreListener.OnCallStateChanged = OnCall;
                CoreListener.OnCallStatsUpdated = OnStats;
                CoreListener.OnRegistrationStateChanged = OnRegistration;

                CoreListener.OnConfiguringStatus = OnConfigurationStatus;

                linphoneCore.EchoCancellationEnabled = true;

                linphoneCore.AddListener(CoreListener);

                //For MTS 4: beamforming_mic_dist_mm=74 beamforming_angle_deg=0 
                //For MTS 7: beamforming_mic_dist_mm =184 beamforming_angle_deg=0 default value in linphonerc

               //linphoneCore.BeamformingMicDist = 74f;
               //linphoneCore.BeamformingEnabled = true;


            }
        }

        private static void OnLog(LoggingService logService, string domain, LogLevel lev, string message)
        {
            string now = DateTime.Now.ToString("hh:mm:ss");
            string log = now + " [";
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
                default:
                    break;
            }
            log += "] (" + domain + ") " + message;

            Log(log);
        }

        private static void OnConfigurationStatus(Core lc, ConfiguringState status, string message)
        {
            Log($"OnConfiguration, status: {status}");
        }

        private static void OnStats(Core lc, Call call, CallStats stats)
        {
            Log("Call stats: " + stats.DownloadBandwidth + " kbits/s / " + stats.UploadBandwidth + " kbits/s");
        }

        private static void OnCall(Core lc, Call lcall, CallState state, string message)
        {
            CallPCL call = new CallPCL
            {
                UsernameCaller = lcall.RemoteAddress.Username
            };
            CallParams param = linphoneCore.CreateCallParams(lcall);
            foreach (var listenner in LinphoneListenners)
            {
                listenner.OnCall(new CallArgs(call, (int)state, message, param.VideoEnabled));
            }

        }

        /// <summary>
        /// To test codecs
        /// </summary>
        private static void LogCodecs()
        {
            var videoCodecs = LinphoneCore.VideoPayloadTypes;
            var audioCodecs = LinphoneCore.AudioPayloadTypes;

            try
            {
                foreach (PayloadType pt in videoCodecs)
                {
                    if (!pt.Enabled())
                    {
                        pt.Enable(true);
                    }
                    Log($"VIDEO - Payload: {pt.MimeType}/{pt.ClockRate}/{pt.NormalBitrate}, Enabled: {pt.Enabled().ToString()}, IsUsable {pt.IsUsable}");

                }
                foreach (PayloadType pt in audioCodecs)
                {
                    if (!pt.Enabled())
                    {
                        pt.Enable(true);
                    }

                    Log($"AUDIO - Payload: {pt.MimeType}/{pt.ClockRate}/{pt.NormalBitrate}, Enabled: {pt.Enabled().ToString()}, IsUsable {pt.IsUsable}");
                }

            }
            catch (Exception e)
            {
                // e.printStackTrace();
            }
        }

        private static void OnGlobal(Core lc, GlobalState gstate, string message)
        {
            Log("LINPHONE - Global state changed -> " + gstate);
        }

        private static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"LINPHONE MANAGER: {message}");
        }

        public static async Task LinphoneCoreIterateAsync()
        {
            while (true)
            {

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    //Log("JobScheduler iteration");
                    LinphoneCore.Iterate();
                    // GC.Collect();

                });

                await Task.Delay(50);
            }
        }

        private static void OnRegistration(Core lc, ProxyConfig config, RegistrationState state, string message)
        {
            Log($"Register, state - {state}, Username - {config.FindAuthInfo().Username}, domain - {config.Domain}");
            if(LinphoneListenners != null)
            {
                foreach (var listenner in LinphoneListenners)
                {
                    listenner.OnRegistration((RegistrationStatePCL)state, message);
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
            bool isMock)
        {

            try
            {

                var prop = Xamarin.Forms.Application.Current.Properties;

                var authInfo = Factory.Instance.CreateAuthInfo(username, null, password, null, null, domain);
                LinphoneCore.AddAuthInfo(authInfo);



                var proxyConfig = LinphoneCore.CreateProxyConfig();
                var identity = Factory.Instance.CreateAddress($"sip:{username}@{domain}");
                if (!isMock)
                {
                    identity = Factory.Instance.CreateAddress($"sip:sample@domain.tld");
                }
                else
                {
                    proxyConfig.SetCustomHeader("Mobile-IMEI", imei);
                    proxyConfig.SetCustomHeader("MyName", myName);
                }
                identity.Transport = TransportType.Tcp;
                identity.Username = username;
                identity.Domain = domain;
                proxyConfig.Edit();


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



        public void AddLinphoneListenner(ILinphneListenner linphneListenner)
        {
            LinphoneListenners.Add(linphneListenner);
        }

        public void RemoveLinphoneListenner(ILinphneListenner linphneListenner)
        {
            LinphoneListenners.Remove(linphneListenner);
        }


        #endregion

    }
}
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LibLinphone.Android.LinphoneUtils
{
    public static class Utils
    {
        public static void TraceException(Exception ex,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            Debug.WriteLine($"WARNING - Managed Exception: {callingMethod} at line {callingFileLineNumber} in file {callingFilePath}\n{ex.Message}\n{ex.StackTrace}");
        }
    }
}
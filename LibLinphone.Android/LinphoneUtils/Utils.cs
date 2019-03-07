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
            var className = callingFilePath;
            
            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if(lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
                className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
            
            Log($"Managed Exception in {className}.{callingMethod}:{callingFileLineNumber} -> {ex.Message}\n{ex.StackTrace}");
        }

        public static void Log(string message,
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
                message = $"[{className}.{callingMethod}:{callingFileLineNumber}] - D - {message}";
            }

            Debug.WriteLine(message);
        }
    }
}
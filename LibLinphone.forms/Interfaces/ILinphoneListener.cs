namespace LibLinphone.forms.Interfaces
{
    public interface ILinphoneListener
    {
        void OnRegistration(RegistrationStatePCL statePCL, string message);
        void OnError(ErrorTypes type);
        void OnCall(CallArgs args);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="recipient">destinatario</param>
        /// <param name="text"></param>
        /// <param name="pandaHeader">Type of recived message</param>
        /// <param name="koalaHeader"></param>
        void OnMessaggeRecived(string recipient, string text, string pandaHeader, string koalaHeader);
    }

    public enum ErrorTypes
    {
        CoreIterateFailed
    }

    public struct PandaValues
    {
        public const string Black = "black"; //allert message
        public const string White = "white"; // normal message
        public const string Blue = "blue"; // event notificatio or sys sync
        public const string Command = "command"; // actuation action
        public const string Gray = "gray"; // base64 recived
    }

    /// <summary>
	/// LinphoneRegistrationState describes proxy registration states. 
	/// </summary>
	public enum RegistrationStatePCL
    {
        /// <summary>
        /// Initial state for registrations. 
        /// </summary>
        None = 0,
        /// <summary>
        /// Registration is in progress. 
        /// </summary>
        Progress = 1,
        /// <summary>
        /// Registration is successful. 
        /// </summary>
        Ok = 2,
        /// <summary>
        /// Unregistration succeeded. 
        /// </summary>
        Cleared = 3,
        /// <summary>
        /// Registration failed. 
        /// </summary>
        Failed = 4,
    }

}

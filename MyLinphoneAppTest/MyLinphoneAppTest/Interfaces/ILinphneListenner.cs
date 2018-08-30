using MyLinphoneAppTes.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyLinphoneAppTest.Interfaces
{
    public interface ILinphneListenner
    {
        void OnRegistration(RegistrationStatePCL statePCL, string message);
        void OnCall(CallArgs args);
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

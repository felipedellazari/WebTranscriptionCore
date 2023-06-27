using Microsoft.Win32.SafeHandles;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WebTranscriptionCore {

	public class ImpersonationHelper {
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

		private const int LOGON32_PROVIDER_DEFAULT = 0;
		private const int LOGON32_LOGON_INTERACTIVE = 2;
		private const int LOGON32_LOGON_NETWORK_CLEARTEXT = 8;
		private static SafeAccessTokenHandle safeAccessTokenHandle;

		public static void Impersonate(NetworkCredential credential, Action action) {
			LogonUser(credential.UserName, credential.Domain, credential.Password, LOGON32_LOGON_NETWORK_CLEARTEXT, LOGON32_PROVIDER_DEFAULT, out safeAccessTokenHandle);
			WindowsIdentity.RunImpersonated(safeAccessTokenHandle, action);
		}
	}

}
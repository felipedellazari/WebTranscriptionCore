using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WebTranscriptionCore {
	public static class Processes {
		[StructLayout(LayoutKind.Sequential)]
		struct RM_UNIQUE_PROCESS {
			public int dwProcessId;
			public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
		}

		const int RmRebootReasonNone = 0;
		const int CCH_RM_MAX_APP_NAME = 255;
		const int CCH_RM_MAX_SVC_NAME = 63;

		enum RM_APP_TYPE {
			RmUnknownApp = 0,
			RmMainWindow = 1,
			RmOtherWindow = 2,
			RmService = 3,
			RmExplorer = 4,
			RmConsole = 5,
			RmCritical = 1000
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct RM_PROCESS_INFO {
			public RM_UNIQUE_PROCESS Process;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
			public string strAppName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
			public string strServiceShortName;

			public RM_APP_TYPE ApplicationType;
			public uint AppStatus;
			public uint TSSessionId;
			[MarshalAs(UnmanagedType.Bool)]
			public bool bRestartable;
		}

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames, uint nApplications, [In] RM_UNIQUE_PROCESS[] rgApplications, uint nServices, string[] rgsServiceNames);

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
		static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

		[DllImport("rstrtmgr.dll")]
		static extern int RmEndSession(uint pSessionHandle);

		[DllImport("rstrtmgr.dll")]
		static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, ref uint lpdwRebootReasons);

		/// <summary>
		/// Find out what process(es) have a lock on the specified file.
		/// </summary>
		/// <param name="path">Path of the file.</param>
		/// <returns>Processes locking the file</returns>
		/// <remarks>
		/// See also: http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
		/// </remarks>
		public static List<Process> WhoIsLocking(string path) {
			List<Process> processes = new List<Process>();

			string key = Guid.NewGuid().ToString();
			int res = RmStartSession(out uint handle, 0, key);
			if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

			try {
				const int ERROR_MORE_DATA = 234;
				uint pnProcInfo = 0;
				uint lpdwRebootReasons = RmRebootReasonNone;

				res = RmRegisterResources(handle, 1, new string[] { path }, 0, null, 0, null);
				if (res != 0) throw new Exception("Could not register resource.");

				// Get the amount of processes
				res = RmGetList(handle, out uint pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);
				if (res == ERROR_MORE_DATA) {
					// Create an array to store the process results
					RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
					pnProcInfo = pnProcInfoNeeded;

					// Get the list
					res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
					if (res == 0) {
						for (int i = 0; i < pnProcInfo; i++) {
							try {
								processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
							} catch (ArgumentException) { } // in case the process is no longer running
						}
					} else throw new Exception("Could not list processes locking resource.");
				} else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
			} finally {
				RmEndSession(handle);
			}
			return processes;
		}
	}
}

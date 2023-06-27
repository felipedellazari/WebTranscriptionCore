using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public static class CredentialsHelper {
		public static NetworkCredential GetCredential() {
			//var usr = provider.First(x => x.Name.Contains("User")).Value;
			//var psw = CryptoHelper.Base64Decrypt(provider.First(x => x.Name.Contains("Password")).Value);
			var usr = "netuser@kenta";
			var psw = "ccMGs+q6FFOUgUCHzpkTWw==";

			return new NetworkCredential { UserName = usr, Password = psw, Domain = "" };
		}
	}

}
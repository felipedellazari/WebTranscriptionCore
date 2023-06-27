using Microsoft.Extensions.Configuration;
using System;

namespace WebTranscriptionCore {

	public class LoginProvider {

		public ILoginProvider Provider;

		public UserK UserK => Provider.UserK;

		public string ErrorMsg => Provider.ErrorMsg;

		public LoginProvider(IConfiguration cfg) {
			Type t = Type.GetType("WebTranscriptionCore." + BLL.GetConfig("Login_Provider", cfg));
			if (t == null) throw new Exception("Login provider não encontrado.");
			Provider = (ILoginProvider)Activator.CreateInstance(t, new object[] { cfg });
		}

		public bool Authenticate(string user, string pwd) => Provider.Authenticate(user, pwd);

		public bool AuthenticateOnRegistry(string user, string pwd) => Provider.AuthenticateOnRegistry(user, pwd);


	}
}
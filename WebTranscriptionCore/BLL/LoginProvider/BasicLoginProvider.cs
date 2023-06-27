using Kenta.Utils;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace WebTranscriptionCore {
	public class BasicLoginProvider : BaseClass, ILoginProvider {

		public BasicLoginProvider(IConfiguration cfg) : base(cfg) { }

		public UserK UserK { get; set; }

		public string ErrorMsg { get; set; }

		public bool Authenticate(string user, string pwd) {
			UserK userK = UserK.GetByPassword(cfg, user, pwd);

			
			//colombo
			
			//if (userK!= null && !userK.ValidateLicenseAsync().Equals("Sucesso"))
			//{
			//	ErrorMsg = "Erro ao validar licença do usuário.";
			//	return false;
			//}
			
			////apaga sessoes de outros usuarios
			//if (userK != null)
			//{
			//	userK.DeleteOldSessions();
			//}

			////cria a nova sessao
			//if (userK != null)
			//{
			//	userK.CreateSession();
			//}
			

			if (userK == null)
			{
				ErrorMsg = "Usuário ou senha inválido.";
				return false;
			}
			else
			{
				UserK = userK;
				return true;
			}
		}

		public bool AuthenticateOnRegistry(string user, string pwd)
		{
			UserK userK = UserK.GetByPassword(cfg, user, pwd);

			if (userK != null)
			{
				userK.DeleteOldSessions();
			}

			if (userK != null)
			{
				userK.CreateSession();
			}

			if (userK == null)
			{
				ErrorMsg = "Usuário ou senha inválido.";
				return false;
			}
			else
			{
				UserK = userK;
				return true;
			}
		}
	}
}
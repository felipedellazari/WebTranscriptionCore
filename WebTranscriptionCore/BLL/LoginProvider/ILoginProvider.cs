namespace WebTranscriptionCore {
	public interface ILoginProvider {

		UserK UserK { get; set; }

		string ErrorMsg { get; set; }

		bool Authenticate(string user, string _pwd);

		bool AuthenticateOnRegistry(string user, string _pwd);
		
	}
}
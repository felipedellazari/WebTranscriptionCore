using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class UserModel {
		[Required]
		[Display(Name = "Usuário")]
		public string User { get; set; }

		[Required]
		[Display(Name = "Senha")]
		[DataType(DataType.Password)]
		public string Password { get; set; }
	}
}
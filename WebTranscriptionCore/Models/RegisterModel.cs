using System;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class RegisterModel {
		[Required(ErrorMessage = "Campo Usuário é obrigatório")]
		[Display(Name = "Usuário")]
		public string User { get; set; }

		[Required(ErrorMessage = "Campo Senha é obrigatório")]
		[Display(Name = "Senha")]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[Required(ErrorMessage = "Campo Licença é obrigatório")]
		[Display(Name = "Licença")]
		public string License { get; set; }
	}
}
using System.ComponentModel;

namespace WebTranscriptionCore {

	public enum FlowStepStatus {
		[Description("Aguardando")]
		Waiting = 1,
		[Description("Ativa")]
		Active = 2,
		[Description("Rejeitada")]
		Revoked = 3,
		[Description("Pausada")]
		Paused = 4,
		[Description("Em execução")]
		Executing = 5,
		[Description("Completa")]
		Done = 6,
		[Description("Cancelada")]
		Canceled = 7,
		[Description("Reaberta")]
		Reopened = 8,
		[Description("Retificar")]
		Correct = 9,
		[Description("Retornada")]
		Returned = 10,
		[Description("Ratificar")]
		Ratified = 11,
		[Description("Atribuído ao usuário")]
		UserDelegate = 12,
		[Description("Atribuído ao perfil")]
		ProfileDelegate = 13,
		ImportedWeb = 14
	}

	public enum SessionStatus {
		[Description("Aguardando")]
		Waiting = 1,
		[Description("Em Gravação")]
		Recording = 2,
		[Description("Em Transcrição")]
		Transcribing = 3,
		[Description("Completa")]
		Complete = 4,
		[Description("Cancelada")]
		Canceled = 5,
	}

	public enum TranscribedInEnum {
		NotTranscribed = 0,
		Desktop = 1,
		Web = 2
	}

	public enum ScopeCfg {
		Global = 1,
		Local = 2,
		User = 4,
	}
}
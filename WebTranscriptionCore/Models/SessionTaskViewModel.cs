using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class SessionTaskViewModel {
		public SessionTaskViewModel() { }
		public long CurrentUserid { get; set; }
		public int ShowReSend { get; set; }
		public string AutoTrancriptionByTask { get; set;}
		public string SessionAutoTranscription { get; set; }
		public IEnumerable<SessionTask> SessionTaskList { get; set; }

		//Filtros
		[Display(Name = "Data Sessão")]
		public DateTime? DateSessionFilter { get; set; }

		[Display(Name = "Hora Sessão")]
		public string TimeSessionFilter { get; set; }

		[Display(Name = "Status Tarefa")]
		public string StatusTaskFilter { get; set; }

		[Display(Name = "Usuário")]
		public string UserFilter { get; set; }

		public IEnumerable<IdName> StatusTaskListFilter { get; set; }

		public IEnumerable<IdName> UserFilterListFilter { get; set; }
	}
}
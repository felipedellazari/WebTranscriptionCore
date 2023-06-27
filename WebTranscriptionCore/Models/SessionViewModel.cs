using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class SessionViewModel {
		public SessionViewModel() { }
		public long CurrentUserid { get; set; }
		public int ShowReSend { get; set; }
		public IEnumerable<Session> SessionList { get; set; }

		//Filtros
		[Display(Name = "De")]
		public DateTime? DateBeginSessionFilter { get; set; }

		[Display(Name = "Até")]
		public DateTime? DateEndSessionFilter { get; set; }

		[Display(Name = "Número")]
		public string NumberSessionFilter { get; set; }

		[Display(Name = "Descrição")]
		public string DescriptionSessionFilter { get; set; }

		[Display(Name = "Local de gravação")]
		public string PlaceSessionFilter { get; set; }
		public IEnumerable<IdName> PlaceFilterListFilter { get; set; }
	}
}
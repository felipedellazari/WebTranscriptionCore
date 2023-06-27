using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebTranscriptionCore;

namespace WebTranscriptionCore
{
	public class ReportProductivityViewModel
	{
		public ReportProductivityViewModel() { }

		public IEnumerable<ReportProductivity> ReportList { get; set; }

		//Filtros

		[Display(Name = "De")]
		public DateTime? DateBeginSessionFilter { get; set; }

		[Display(Name = "Até")]
		public DateTime? DateEndSessionFilter { get; set; }

		[Display(Name = "Usuário")]
		public string UserFilter { get; set; }

		public IEnumerable<IdName> UserFilterListFilter { get; set; }
	}
}
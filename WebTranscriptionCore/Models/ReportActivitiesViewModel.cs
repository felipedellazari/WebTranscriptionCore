using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore
{
	public class ReportActivitiesViewModel
	{
		public ReportActivitiesViewModel() { }
		
		public IEnumerable<ReportActivities> ReportList { get; set; }

		//Filtros

		[Display(Name = "De")]
		public DateTime? DateBeginSessionFilter { get; set; }

		[Display(Name = "Até")]
		public DateTime? DateEndSessionFilter { get; set; }

		[Display(Name = "Operador")]
		public string UserFilter { get; set; }

		public IEnumerable<IdName> UserFilterListFilter { get; set; }


	}
}
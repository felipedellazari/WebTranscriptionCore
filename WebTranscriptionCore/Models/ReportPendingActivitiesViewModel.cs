using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore
{
	public class ReportPendingActivitiesViewModel
	{
		public ReportPendingActivitiesViewModel() { }

		public IEnumerable<ReportPendingActivities> ReportList { get; set; }

		//Filtros

		[Display(Name = "De")]
		public DateTime? DateBeginSessionFilter { get; set; }

		[Display(Name = "Até")]
		public DateTime? DateEndSessionFilter { get; set; }

		[Display(Name = "Encarregado")]

		public string UserFilter { get; set; }

		public IEnumerable<IdName> UserFilterListFilter { get; set; }

		public IEnumerable<IdName> StatusTaskListFilter { get; set; }

	}
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class PlaceViewModel {
		public int Id { get; set; }
		public int Active { get; set; }
		public int? JudgeId1 { get; set; }
		public int? JudgeId2 { get; set; }
		public int? ParentId { get; set; }
		public int Degree { get; set; }
		public string ForeignKey { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class AttendantRoleViewModel {
		public int Id { get; set; }
		public int Active { get; set; }
		public string ForeignKey { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}
}
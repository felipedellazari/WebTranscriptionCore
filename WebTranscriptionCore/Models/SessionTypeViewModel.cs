using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class SessionTypeViewModel {
		public int Id { get; set; }
		public int Active { get; set; }
		public string ForeignKey { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Subjects { get; set; }
		public string Roles { get; set; }
	}
}
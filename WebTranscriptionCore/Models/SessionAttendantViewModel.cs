using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class SessionAttendantViewModel {
		public int Id { get; set; }
		public int SessionId { get; set; }
		public int RoleId { get; set; }
		public int? AttendantId { get; set; }
		public string Name { get; set; }

		public virtual AttendantViewModel Attendant { get; set; }
		public virtual AttendantRoleViewModel Role { get; set; }
	}
}
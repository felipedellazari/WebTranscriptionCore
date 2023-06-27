using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class AttendantViewModel {
		public int? Id { get; set; }
		public int Active { get; set; }
		public int? RoleId { get; set; }
		[Required(ErrorMessage = "O preenchimento do nome é obrigatório.")] 
		public string Name { get; set; }
		public string Party { get; set; }
		public string ForeignKey { get; set; }
		public string AttendandRoleId { get; set; }
		public IEnumerable<IdName> AttendandRoleList { get; set; }

		public virtual AttendantRoleViewModel Role { get; set; }

		public IEnumerable<Attendant> AttendantList { get; set; }

		//Filtros
		[Display(Name = "Nome")]
		public string NameAttendantFilter { get; set; }

	}
}
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace WebTranscriptionCore {
	public class SessionAttendant : BaseClass {
		public int Id { get; set; }
		public int SessionId { get; set; }
		public int RoleId { get; set; }
		public int? AttendantId { get; set; }
		public string Name { get; set; }
		public Attendant Attendant { get; set; }
		public AttendantRole AttendantRole { get; set; }

		public static IEnumerable<SessionAttendant> List(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId) {

			Dictionary<string, object> args = new Dictionary<string, object>();

			string sql = @"SELECT sa.ID,
								sa.SESSIONID,
								sa.ATTENDANTID,
								sa.ROLEID,
								sa.NAME,
								a.ACTIVE,
								a.DEFAULTROLEID,
								a.NAME as ATTENDANTNAME,
								a.FOREIGNKEY,
								ar.NAME as ATTENDANTROLENAME
						FROM SessionAttendant sa
						LEFT JOIN Attendant a on sa.ATTENDANTID = a.ID
						LEFT JOIN AttendantRole ar on a.DEFAULTROLEID = ar.ID
						WHERE sa.SESSIONID = :sessionId";

			List<SessionAttendant> list = new List<SessionAttendant>();
			IEnumerable<SessionAttendantDB> lst = BLL.Cnn(cfg).Query<SessionAttendantDB>(sql, new { sessionId });

			foreach (SessionAttendantDB item in lst) {
				var attendantRole = new AttendantRole() {
					Name = item.AttendantRoleName,
				};
				var attendant = new Attendant() {
					Id = item.AttendantId,
					Active = item.Active,
					DefaultRoleId = item.DefaultRoleId,
					Name = item.AttendantName,
					ForeignKey = item.ForeignKey,
					AttendantRole = attendantRole
				};

				list.Add(new SessionAttendant() {
					Id = item.Id,
					SessionId = item.SessionId,
					RoleId = item.RoleId,
					AttendantId = item.AttendantId,
					Name = item.Name,
					Attendant = attendant,
					AttendantRole = attendantRole
				});
			}

			return list;
		}

		public static IEnumerable<IdName> ListAll(IConfiguration cfg, long sessionId) {
			string sql = $@"SELECT sa.ID, a.NAME FROM ""SESSIONATTENDANT"" sa
				JOIN ""ATTENDANT"" a ON sa.attendantid = a.Id
				WHERE sessionId = :sessionId ORDER BY Name";
			IEnumerable<IdName> lst = BLL.Cnn(cfg).Query<IdName>(sql, new { sessionId });
			return lst;
		}

		private class SessionAttendantDB {
			public int Id { get; set; }
			public int SessionId { get; set; }
			public int RoleId { get; set; }
			public int? AttendantId { get; set; }
			public string Name { get; set; }
			public int Active { get; set; }
			public int? DefaultRoleId { get; set; }
			public string AttendantName { get; set; }
			public string ForeignKey { get; set; }
			public string AttendantRoleName { get; set; }
		}
	}
}
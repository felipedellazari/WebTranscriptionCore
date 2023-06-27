using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace WebTranscriptionCore {
	public class Attendant : BaseClass {
		public int? Id { get; set; }
		public int Active { get; set; }
		public int? DefaultRoleId { get; set; }
		public string Name { get; set; }
		public string ForeignKey { get; set; }
		public string Party { get; set; }
		public string AttendantRoleName { get; set; }
		public AttendantRole AttendantRole { get; set; }

		public Attendant() { }

		public Attendant(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public Attendant(long id, IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) {

			string sql = @"select  a.id, 
								a.name, 
								a.party,
								a.defaultroleid,
								ar.name as AttendantRoleName 
						from attendant a
						left join attendantrole ar on a.defaultroleid = ar.id
						where a.active = 1 and a.id = :id";
			Attendant attendant = BLL.Cnn(cfg).QueryFirst<Attendant>(sql, new { id });
			Id = attendant.Id;
			Name = attendant.Name;
			Party = attendant.Party;
			DefaultRoleId = attendant.DefaultRoleId;
			AttendantRoleName = attendant.AttendantRoleName;
		}

		public static IEnumerable<IdName> ListAll(IConfiguration cfg) {
			string sql = @"SELECT ID, NAME FROM ""ATTENDANT"" WHERE Active = 1 ORDER BY Name";
			IEnumerable<IdName> lst = BLL.Cnn(cfg).Query<IdName>(sql, new { });
			return lst;
		}

		public int ListCount(string name) {
			int count = 0;
			List(true, ref count, null, null, name);
			return count;
		}

		public IEnumerable<Attendant> List(int? between1, int? between2, string name) {
			int count = 0;
			return List(false, ref count, between1, between2, name);
		}

		private IEnumerable<Attendant> List(bool bCount, ref int iCount, int? between1, int? between2, string name) {
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @" select	a.id, 
											a.name, 
											a.party,
											a.defaultroleid,
											ar.name as attendantRoleName";

			string sqlFrom = @" from attendant a
									left join attendantrole ar on a.defaultroleid = ar.id ";

			string sqlWhere = " where a.active = 1";

			if (name != null && name != "") {
				name = name.ToUpper();
				sqlWhere += @" AND UPPER(a.name) LIKE '%' || :name || '%'";
				args.Add("name", name);
			}

			string sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere : "SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY a.name) RowNumCol " + sqlFrom + sqlWhere + ") tab WHERE RowNumCol BETWEEN :between1 AND :between2";
			args.Add("between1", between1);
			args.Add("between2", between2);

			List<Attendant> list = new List<Attendant>();

			if (bCount) {

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			} else {

				IEnumerable<Attendant> lst = BLL.Cnn(cfg).Query<Attendant>(sql, args);
				foreach (Attendant item in lst) {
					list.Add(new Attendant() {
						Id = item.Id,
						Name = item.Name,
						DefaultRoleId = item.Id,
						Party = item.Party,
						AttendantRoleName = item.AttendantRoleName,
						AttendantRole = new AttendantRole() { Name = item.AttendantRoleName }
					});
				}
			}
			return list;
		}

		public static bool Insert(IConfiguration cfg, IEnumerable<Claim> claims, Attendant attendant) {

			string sql = @"INSERT INTO ATTENDANT (ID, NAME, PARTY, DEFAULTROLEID, ACTIVE) 
				VALUES (SQ_ATTENDANT.NEXTVAL, :name, :party, :defaultroleid, 1)";

			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("name", attendant.Name);
			args.Add("party", attendant.Party);
			args.Add("defaultroleid", attendant.DefaultRoleId);

			BLL.Cnn(cfg).Execute(sql, args);

			return true;
		}

		public static bool Update(IConfiguration cfg, IEnumerable<Claim> claims, Attendant attendant) {

			string sql = @"UPDATE ATTENDANT SET NAME = :name, PARTY = :party, DEFAULTROLEID = :defaultroleid WHERE ID = :id ";

			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("name", attendant.Name);
			args.Add("party", attendant.Party);
			args.Add("defaultroleid", attendant.DefaultRoleId);
			args.Add("id", attendant.Id);

			BLL.Cnn(cfg).Execute(sql, args);

			return true;
		}

		public static bool Delete(IConfiguration cfg, IEnumerable<Claim> claims, long id) {

			string sql = @"UPDATE ATTENDANT SET ACTIVE = 0 WHERE ID = :id ";

			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("id", id);

			BLL.Cnn(cfg).Execute(sql, args);

			return true;
		}

	}
}
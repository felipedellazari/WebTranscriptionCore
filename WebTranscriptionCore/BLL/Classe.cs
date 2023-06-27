using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace WebTranscriptionCore {
	public class Classe : BaseClass {
		public int? Id { get; set; }
		public int Active { get; set; }
		public string Name { get; set; }

		public static IEnumerable<IdName> ListAll(IConfiguration cfg) {
			string sql = @"SELECT ID, NAME FROM ""CLASSE"" WHERE Active = 1 ORDER BY Name";
			IEnumerable<IdName> lst = BLL.Cnn(cfg).Query<IdName>(sql, new { });
			return lst;
		}
	}
}
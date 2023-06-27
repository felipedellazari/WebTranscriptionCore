using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace WebTranscriptionCore
{

	public class ReportActivitiesDAL : BaseClass
	{


		public string Nome_Operador { get; set; }

		public DateTime Data_Sessao { get; set; }

		public string Tipo_Sessao { get; set; }

		public string Tarefa { get; set; }

		public ReportActivitiesDAL() { }

		public ReportActivitiesDAL(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
	}
}
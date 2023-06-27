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

	public class ReportAbsenceDAL : BaseClass
	{


		public string Nome { get; set; }

		public DateTime Data_Inicio { get; set; }

		public string Data_Fim { get; set; }

		public string Motivo { get; set; }

		public ReportAbsenceDAL() { }

		public ReportAbsenceDAL(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
	}
}
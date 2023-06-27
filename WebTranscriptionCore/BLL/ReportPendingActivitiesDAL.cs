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

	public class ReportPendingActivitiesDAL : BaseClass
	{


		public string Descricao { get; set; }

		public FlowStepStatus Status { get; set; }

		public string Encarregado { get; set; }

		public DateTime Data_criacao { get; set; }



		public ReportPendingActivitiesDAL() { }

		public ReportPendingActivitiesDAL(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
	}
}
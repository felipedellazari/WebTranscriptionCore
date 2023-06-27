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

	public class ReportProductivityDAL : BaseClass
	{


		public string Sessao { get; set; }

		public long TempoTotal { get; set; }

		public long TempoMedio { get; set; }

		public DateTime Data_criacao { get; set; }

		public string Tarefa { get; set; }
		public string QuantidadeDetarefas { get; set; }

		public ReportProductivityDAL() { }

		public ReportProductivityDAL(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
	}
}
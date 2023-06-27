using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static WebTranscriptionCore.OnlineProvider;

namespace WebTranscriptionCore
{
	public class ReportProductivity : BaseClass
	{

		public string Sessao { get; set; }

		public long TempoTotal { get; set; }

		public long TempoMedio { get; set; }

		public string Tarefa { get; set; }

		public DateTime Data_criacao { get; set; }

		public string QuantidadeDetarefas { get; set; }


		public int ListCount(DateTime? dateBegin = null, DateTime? dateEnd = null, long? _UserFilter = null)
		{
			int count = 0;
			List(true, ref count, null, null, dateBegin, dateEnd, _UserFilter);
			return count;
		}

		public IEnumerable<ReportProductivity> List(int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, long? _UserFilter)
		{
			int count = 0;
			return List(false, ref count, between1, between2, dateBegin, dateEnd, _UserFilter);
		}

		public ReportProductivity(IConfiguration cfg) : base(cfg) { }

		public ReportProductivity(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
		public ReportProductivity() { }


		private IEnumerable<ReportProductivity> List(bool bCount, ref int iCount, int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, long? _UserFilter)
		{
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @"SELECT s.""NUMBER"" Sessao,
								SUM(st.Duration) TempoTotal, 
								FLOOR(AVG(st.Duration)) TempoMedio, 
								fs.Name Tarefa, 
								s.ExpectedDate Data_criacao,
								COUNT(*) QuantidadeDetarefas";

			string sqlFrom = @" FROM SessionTask st
								INNER JOIN ""SESSION"" s ON s.ID = st.SessionId
								INNER JOIN FlowStep fs ON st.StepId = fs.Id
								INNER JOIN ""USER"" u ON st.AssignedUserId = u.Id";

			//zero sendo passado como fixo, tem que pegar o id do usuário do banco selecionado na dropdown dos usuarios
			string sql = "";

			string sqlWhere = "";
			
			if(dateBegin != null && dateEnd != null && _UserFilter != null)
			{
				sqlWhere += @" WHERE s.Active = 1 AND st.AssignedUserId = :UserId AND s.ExpectedDate BETWEEN :dtBegin AND :dtEnd GROUP BY st.AssignedUserId, s.""NUMBER"", s.ExpectedDate, fs.Name ORDER BY s.ExpectedDate DESC";
				args.Add("UserId", _UserFilter.Value);
				args.Add("dtBegin", dateBegin.Value.Date);
				args.Add("dtEnd", dateEnd.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
			}
			else if (_UserFilter == null)
			{
				sqlWhere = @" WHERE s.Active = -1 AND st.AssignedUserId = 0 GROUP BY st.AssignedUserId, s.""NUMBER"", s.ExpectedDate, fs.Name ORDER BY s.ExpectedDate DESC";

			}
			else if(dateBegin == null && dateEnd == null && _UserFilter == null)
			{
				sqlWhere = @" WHERE s.Active = 1 AND st.AssignedUserId = 0 GROUP BY st.AssignedUserId, s.""NUMBER"", s.ExpectedDate, fs.Name ORDER BY s.ExpectedDate DESC";
			}
			else if (dateBegin == null && dateEnd == null && _UserFilter != null)
			{
				sqlWhere += @" WHERE s.Active = 1 AND st.AssignedUserId = :UserId GROUP BY st.AssignedUserId, s.""NUMBER"", s.ExpectedDate, fs.Name ORDER BY s.ExpectedDate DESC";
				args.Add("UserId", _UserFilter.Value);				
			}

		
			 sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere :
				"SELECT * FROM (" + sqlSelect + sqlFrom + sqlWhere + ")";
			//args.Add("between1", between1);
			//args.Add("between2", between2);
			
			List<ReportProductivity> list = new List<ReportProductivity>();

			if (bCount)
			{

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			}
			else
			{

				IEnumerable<ReportProductivityDAL> lst = BLL.Cnn(cfg).Query<ReportProductivityDAL>(sql, args);

				list = new List<ReportProductivity>();
				foreach (ReportProductivityDAL item in lst)
				{
					
					string tempoTotalFormatado = new TimeSpan(TempoTotal).ToString(@"hh\:mm\:ss");
					

					list.Add(new ReportProductivity()
					{
						Sessao = item.Sessao,
						TempoTotal = item.TempoTotal,
						TempoMedio = item.TempoMedio,
						Tarefa = item.Tarefa,
						Data_criacao = item.Data_criacao,
						QuantidadeDetarefas = item.QuantidadeDetarefas

					});
				}

				return list;
			}

		}
	}
}

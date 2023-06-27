using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using static WebTranscriptionCore.OnlineProvider;

namespace WebTranscriptionCore
{
	public class ReportActivities : BaseClass
	{

		public string Nome_Operador { get; set; }

		public DateTime Data_Sessao { get; set; }

		public string Tipo_Sessao { get; set; }

		public string Tarefa { get; set; }

		public int ListCount(DateTime? dateBegin = null, DateTime? dateEnd = null, long? _UserFilter = null)
		{
			int count = 0;
			List(true, ref count, null, null, dateBegin, dateEnd, _UserFilter);
			return count;
		}

		public IEnumerable<ReportActivities> List(int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, long? _UserFilter = null)
		{
			int count = 0;
			return List(false, ref count, between1, between2, dateBegin, dateEnd, _UserFilter);
		}

		public ReportActivities(IConfiguration cfg) : base(cfg) { }

		public ReportActivities(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }


		public ReportActivities() { }


		private IEnumerable<ReportActivities> List(bool bCount, ref int iCount, int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, long? _UserFilter)
		{
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @"select u.name Nome_Operador, 
								s.expecteddate Data_Sessao, 
								stype.name Tipo_Sessao, 
								st.description Tarefa";

			string sqlFrom = @"from  sessiontask st
								inner join ""USER"" u on u.Id = st.assigneduserid
							    inner join ""SESSION"" s on s.id = st.sessionid
							    inner join sessiontype stype on stype.id = s.typeid ";

			

			string sqlWhere = " where 1 = 1";

			if (dateBegin != null && dateEnd != null)
			{
				sqlWhere += " AND s.ExpectedDate BETWEEN :dtBegin AND :dtEnd";
				args.Add("dtBegin", dateBegin.Value.Date);
				args.Add("dtEnd", dateEnd.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
			}
			if (_UserFilter != null)
			{
				sqlWhere += " AND u.Id = :UserId";
				args.Add("UserId", _UserFilter);
			}



			string sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere : 
				"SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY s.expecteddate desc) RowNumCol " + sqlFrom + sqlWhere + ") tab";
			//args.Add("between1", between1);
			//args.Add("between2", between2);

			List<ReportActivities> list = new List<ReportActivities>();

			if (bCount)
			{

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			}
			else
			{

				IEnumerable<ReportActivitiesDAL> lst = BLL.Cnn(cfg).Query<ReportActivitiesDAL>(sql, args);

				list = new List<ReportActivities>();
				foreach (ReportActivitiesDAL item in lst)
				{
					
					list.Add(new ReportActivities()
					{						
						Nome_Operador = item.Nome_Operador,
						Tarefa = item.Tarefa,
						Data_Sessao = item.Data_Sessao,
						Tipo_Sessao = item.Tipo_Sessao

					});
				}
			}
			return list;
		}

		}
}

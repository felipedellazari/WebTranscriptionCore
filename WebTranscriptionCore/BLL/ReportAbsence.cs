using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using static WebTranscriptionCore.OnlineProvider;

namespace WebTranscriptionCore
{
	public class ReportAbsence : BaseClass
	{

		public string Nome { get; set; }

		public DateTime Data_Inicio { get; set; }

		public string Data_Fim { get; set; }

		public string Motivo { get; set; }

		public int ListCount(DateTime? dateBegin = null, DateTime? dateEnd = null,long? _UserFilter = null)
		{
			int count = 0;
			List(true, ref count, null, null, dateBegin, dateEnd, _UserFilter);
			return count;
		}

		public IEnumerable<ReportAbsence> List(int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, long? _UserFilter)
		{
			int count = 0;
			return List(false, ref count, between1, between2, dateBegin, dateEnd, _UserFilter);
		}

		public ReportAbsence(IConfiguration cfg) : base(cfg) { }

		public ReportAbsence(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }


		public ReportAbsence() { }


		private IEnumerable<ReportAbsence> List(bool bCount, ref int iCount, int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, long? _UserFilter)
		{
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @"SELECT u.Name Nome,
								a.INITIALDATE Data_Inicio, 
								a.FINALDATE Data_Fim, 
								ar.DESCRIPTION Motivo";

			string sqlFrom = @"FROM ABSENT a 
								INNER JOIN ABSENTREASON ar ON ar.ID = a.REASONID 
								INNER JOIN ""USER"" u ON u.ID = a.USERID";

			string sqlWhere = " where 1 = 1";

			if (dateBegin != null && dateEnd != null)
			{
				sqlWhere += "AND a.FINALDATE BETWEEN :dtBegin AND :dtEnd";				
				args.Add("dtBegin", dateBegin.Value.Date);
				args.Add("dtEnd", dateEnd.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
			}
			if (_UserFilter != null)
			{
				sqlWhere += " AND a.USERID = :UserId";
				args.Add("UserId", _UserFilter);
			}


			string sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere :
				"SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY a.INITIALDATE desc) RowNumCol " + sqlFrom + sqlWhere + ") tab";
			//args.Add("between1", between1);
			//args.Add("between2", between2);

			List<ReportAbsence> list = new List<ReportAbsence>();

			if (bCount)
			{

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			}
			else
			{

				IEnumerable<ReportAbsenceDAL> lst = BLL.Cnn(cfg).Query<ReportAbsenceDAL>(sql, args);

				list = new List<ReportAbsence>();
				foreach (ReportAbsenceDAL item in lst)
				{

					list.Add(new ReportAbsence()
					{
						Nome = item.Nome,
						Data_Inicio = item.Data_Inicio,
						Data_Fim = item.Data_Fim,
						Motivo = item.Motivo

					});
				}
			}
			return list;
		}

	}
}

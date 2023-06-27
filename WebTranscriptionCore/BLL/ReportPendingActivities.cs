using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static WebTranscriptionCore.OnlineProvider;

namespace WebTranscriptionCore
{
	public class ReportPendingActivities : BaseClass
	{


		public string Descricao { get; set; }

		public FlowStepStatus Status { get; set; }

		public string Encarregado { get; set; }
		public DateTime Data_criacao { get; set; }
		public string StatusDesc => Status.GetDescription();


		public int ListCount(long? _UserFilter = null, byte? taskStatus = null, DateTime? dateBegin = null, DateTime? dateEnd = null)
		{
			int count = 0;
			List(true, ref count, null, null, _UserFilter, taskStatus, dateBegin, dateEnd);
			return count;
		}


			public IEnumerable<ReportPendingActivities> List(int? between1, int? between2, long? _UserFilter, byte? taskStatus, DateTime? dateBegin, DateTime? dateEnd)
		{
			int count = 0;
			return List(false, ref count, between1, between2, _UserFilter, taskStatus, dateBegin, dateEnd);
		}

		public ReportPendingActivities(IConfiguration cfg) : base(cfg) { }

		public ReportPendingActivities(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }


		public ReportPendingActivities() { }


		private IEnumerable<ReportPendingActivities> List(bool bCount, ref int iCount, int? between1, int? between2, long? _UserFilter, byte? taskStatus, DateTime? dateBegin, DateTime? dateEnd)
		{
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @"SELECT st.DESCRIPTION Descricao, st.STATUS Status,st.CREATIONDATE Data_criacao, u.Name Encarregado ";

			string sqlFrom = @"FROM SESSIONTASK st INNER JOIN ""USER"" u ON u.ID = st.ASSIGNEDUSERID INNER JOIN ""SESSION"" s ON s.""ID"" = st.SESSIONID ";

			string sqlWhere = "WHERE st.STATUS != 6 AND st.STATUS != 7 AND st.STATUS != 1 AND s.ACTIVE = 1";

			if (_UserFilter != null)
			{
				sqlWhere += " AND st.ASSIGNEDUSERID = :UserId";
				args.Add("UserId", _UserFilter);
			}

			if (dateBegin != null && dateEnd != null)
			{
				sqlWhere += " AND st.CREATIONDATE BETWEEN :dtBegin AND :dtEnd";
				args.Add("dtBegin", dateBegin.Value.Date);
				args.Add("dtEnd", dateEnd.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
			}

			string sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere :
				"SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY st.CREATIONDATE asc) RowNumCol " + sqlFrom + sqlWhere + ") tab";
			//args.Add("between1", between1);
			//args.Add("between2", between2);

			List<ReportPendingActivities> list = new List<ReportPendingActivities>();

			if (taskStatus >= 2)
			{
				List<FlowStepStatus> lSta = new List<FlowStepStatus>();
				if (taskStatus == 2)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Active, FlowStepStatus.Revoked, FlowStepStatus.Paused, FlowStepStatus.Reopened, FlowStepStatus.Correct, FlowStepStatus.Ratified, FlowStepStatus.Executing });
				else if (taskStatus == 3)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Waiting, FlowStepStatus.Active, FlowStepStatus.Executing, FlowStepStatus.Paused, FlowStepStatus.Revoked });
				else if (taskStatus == 4)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Waiting });
				else if (taskStatus == 5)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Active, FlowStepStatus.Executing, FlowStepStatus.Paused, FlowStepStatus.Revoked });
				else if (taskStatus == 6)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Revoked });
				else if (taskStatus == 7)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Done });
				else if (taskStatus == 8)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Canceled });
				sqlWhere += @" AND STATUS IN (" + string.Join(",", lSta.Select(x => ((int)x).ToString())) + ")";
			}

			if (bCount)
			{

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			}
			else
			{

				IEnumerable<ReportPendingActivitiesDAL> lst = BLL.Cnn(cfg).Query<ReportPendingActivitiesDAL>(sql, args);

				list = new List<ReportPendingActivities>();
				foreach (ReportPendingActivitiesDAL item in lst)
				{

					list.Add(new ReportPendingActivities()
					{
						Descricao = item.Descricao,
						Status = (FlowStepStatus)item.Status,
						Data_criacao = item.Data_criacao,
						Encarregado = item.Encarregado	

					});
				}
			}
			return list;
		}

	}
}

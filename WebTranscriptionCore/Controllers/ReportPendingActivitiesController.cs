using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using X.PagedList;
using static WebTranscriptionCore.OnlineProvider;

namespace WebTranscriptionCore.Controllers
{
	public class ReportPendingActivitiesController : Controller
	{
		private readonly IConfiguration cfg;

		public ReportPendingActivitiesController(IConfiguration cfg, IWebHostEnvironment env) => this.cfg = cfg;

		public override void OnActionExecuting(ActionExecutingContext context) {
#if !TSE_DF_DEBUG && !TSE_DF_RELEASE
			context.Result = new RedirectToRouteResult(new RouteValueDictionary(new {
				controller = "Home",
				action = "Index"
			}));
			return;
#endif
		}

		[Authorize(Roles = "1")]
		public ActionResult Index(int? page = 1, string DateBeginSessionFilter = null, string DateEndSessionFilter = null, DateTime? Data_sessao = null, string? UserFilter = null, byte? taskStatus = null, DateTime? dateBegin = null, DateTime? dateEnd = null)
		{
			ReportPendingActivities st = new ReportPendingActivities(cfg);
			ReportPendingActivitiesViewModel model = new ReportPendingActivitiesViewModel();

			long uId = BLL.CurrentUserId(User.Claims);

			DateTime? _DateBeginSessionFilter = (DateBeginSessionFilter.Cleanup() == null || DateBeginSessionFilter.Contains("_")) ? null : (DateTime?)DateTime.ParseExact(DateBeginSessionFilter, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			DateTime? _DateEndSessionFilter = (DateEndSessionFilter.Cleanup() == null || DateEndSessionFilter.Contains("_")) ? null : (DateTime?)DateTime.ParseExact(DateEndSessionFilter, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			long? _UserFilter = UserFilter == "-1" ? null : UserFilter != null ? (long?)Convert.ToInt64(UserFilter) : null;

			int pageCount = st.ListCount(_UserFilter, null, _DateBeginSessionFilter, _DateEndSessionFilter);
			int between2 = BLL.PageSize(cfg) * page ?? 1;
			int between1 = between2 - BLL.PageSize(cfg) + 1;
			model.ReportList = st.List(between1, between2, _UserFilter, taskStatus, _DateBeginSessionFilter, _DateEndSessionFilter);
			model.ReportList = new StaticPagedList<ReportPendingActivities>(model.ReportList, page ?? 1, BLL.PageSize(cfg), pageCount);


			model.DateBeginSessionFilter = _DateBeginSessionFilter;
			model.DateEndSessionFilter = _DateEndSessionFilter;
			model.UserFilterListFilter = new List<IdName>() { new IdName() { Id = -1, Name = "Todos" } }.Concat(UserK.ListAll(cfg));
			model.UserFilter = UserFilter;

			HttpContext.Session.SetString("DateBeginSessionFilter", DateBeginSessionFilter ?? "");
			HttpContext.Session.SetString("DateEndSessionFilter", DateEndSessionFilter ?? "");
			HttpContext.Session.SetString("UserFilter", UserFilter ?? "");

			return View(model);
		}

	}
}


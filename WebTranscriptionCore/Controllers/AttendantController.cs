using Kenta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using X.PagedList;

namespace WebTranscriptionCore {
	public class AttendantController : Controller {

		private readonly IConfiguration cfg;

		public AttendantController(IConfiguration cfg, IWebHostEnvironment env) => this.cfg = cfg;

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
		public ActionResult Index(int? page = 1, string NameAttendantFilter = null) {
			Attendant st = new Attendant(cfg, User.Claims);
			AttendantViewModel model = new AttendantViewModel();

			string _NameAttendantFilter = (NameAttendantFilter.Cleanup() == null || NameAttendantFilter.Contains("_")) ? null : NameAttendantFilter;

			int pageCount = st.ListCount(_NameAttendantFilter);
			int between2 = BLL.PageSize(cfg) * page ?? 1;
			int between1 = between2 - BLL.PageSize(cfg) + 1;
			model.AttendantList = st.List(between1, between2, _NameAttendantFilter);
			model.AttendantList = new StaticPagedList<Attendant>(model.AttendantList, page ?? 1, BLL.PageSize(cfg), pageCount);

			model.NameAttendantFilter = NameAttendantFilter;
			HttpContext.Session.SetString("NameAttendantFilter", NameAttendantFilter ?? "");
			return View(model);
		}

		public ActionResult Create() {
			AttendantViewModel model = new AttendantViewModel();
			model.AttendandRoleList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(AttendantRole.ListAll(cfg));
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(AttendantViewModel model) {
			try {
				int? _AttendantRoleId = model.AttendandRoleId == "-1" ? null : (int?)Convert.ToInt32(model.AttendandRoleId);
				if (_AttendantRoleId == null) {
					ModelState.AddModelError("AttendandRoleId", "Selecione o papel do orador.");
				}
				if (!ModelState.IsValid) {
					model.AttendandRoleList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(AttendantRole.ListAll(cfg));
					return View(model);

				}
				Attendant attendant = new Attendant() {
					Name = model.Name,
					Party = model.Party,
					DefaultRoleId = _AttendantRoleId
				};
				Attendant.Insert(cfg, User.Claims, attendant);
				return RedirectToAction(nameof(Index));

			} catch {
				model.AttendandRoleList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(AttendantRole.ListAll(cfg));
				return View(model);
			}
		}

		public ActionResult Edit(int id) {
			Attendant attendant = new Attendant(id, cfg, User.Claims);
			AttendantViewModel model = new AttendantViewModel() {
				Id = attendant.Id,
				Name = attendant.Name,
				Party = attendant.Party,
				AttendandRoleId = attendant.DefaultRoleId.ToString(),
				AttendandRoleList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(AttendantRole.ListAll(cfg))
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(AttendantViewModel model) {
			try {
				int? _AttendantRoleId = model.AttendandRoleId == "-1" ? null : (int?)Convert.ToInt32(model.AttendandRoleId);
				if (_AttendantRoleId == null) {
					ModelState.AddModelError("AttendandRoleId", "Selecione o papel do orador.");
				}
				if (!ModelState.IsValid) {
					model.AttendandRoleList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(AttendantRole.ListAll(cfg));
					return View(model);

				}
				Attendant attendant = new Attendant() {
					Id = model.Id,
					Name = model.Name,
					Party = model.Party,
					DefaultRoleId = _AttendantRoleId
				};
				Attendant.Update(cfg, User.Claims, attendant);
				return RedirectToAction(nameof(Index));

			} catch {
				model.AttendandRoleList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(AttendantRole.ListAll(cfg));
				return View(model);
			}
		}

		[HttpGet]
		public ActionResult Delete(int id) {
			Attendant attendant = new Attendant(id, cfg, User.Claims);
			AttendantViewModel model = new AttendantViewModel() {
				Id = attendant.Id,
				Name = attendant.Name,
				Party = attendant.Party,
				Role = new AttendantRoleViewModel() { Name = attendant.AttendantRoleName }
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Delete(AttendantViewModel model) {
			try {
				Attendant.Delete(cfg, User.Claims, Convert.ToInt64(model.Id));
				return RedirectToAction(nameof(Index));
			} catch {
				return View();
			}
		}
	}
}
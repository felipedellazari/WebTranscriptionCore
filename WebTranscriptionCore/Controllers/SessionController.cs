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
	public class SessionController : Controller {

		private readonly IConfiguration cfg;
		private readonly string[] browsers = new[] { "Chrome", "Firefox", "Edge", "Safari" };
		private NetworkCredential credential;
		private string userAgent;
		private string mimeType;
		private string filePath;

		public SessionController(IConfiguration cfg, IWebHostEnvironment env) => this.cfg = cfg;

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
		public ActionResult Index(int? page = 1, string DateBeginSessionFilter = null, string DateEndSessionFilter = null, string DescriptionSessionFilter = null, string NumberSessionFilter = null, string PlaceSessionFilter = null) {
			Session st = new Session(cfg, User.Claims);
			SessionViewModel model = new SessionViewModel();
			long uId = BLL.CurrentUserId(User.Claims);

			DateTime? _DateBeginSessionFilter = (DateBeginSessionFilter.Cleanup() == null || DateBeginSessionFilter.Contains("_")) ? null : (DateTime?)DateTime.ParseExact(DateBeginSessionFilter, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			DateTime? _DateEndSessionFilter = (DateEndSessionFilter.Cleanup() == null || DateEndSessionFilter.Contains("_")) ? null : (DateTime?)DateTime.ParseExact(DateEndSessionFilter, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			long? _PlaceSessionFilter = PlaceSessionFilter == "-1" ? null : (long?)Convert.ToInt64(PlaceSessionFilter);
			string _NumberSessionFilter = (NumberSessionFilter.Cleanup() == null || NumberSessionFilter.Contains("_")) ? null : NumberSessionFilter;
			string _DescriptionSessionFilter = (DescriptionSessionFilter.Cleanup() == null || DescriptionSessionFilter.Contains("_")) ? null : DescriptionSessionFilter;

			//verifica se o usuario logado possui permissao para acessar sessoes sigilosas
			string extraWhereConfidential = new UserK(uId, cfg).IsConfidentialAllowed() ? "" : " AND NVL(s.Confidential, 0) = 0 ";

			int pageCount = st.ListCount(_DateBeginSessionFilter, _DateEndSessionFilter, _NumberSessionFilter, _DescriptionSessionFilter, _PlaceSessionFilter, extraWhereConfidential);
			int between2 = BLL.PageSize(cfg) * page ?? 1;
			int between1 = between2 - BLL.PageSize(cfg) + 1;
			model.SessionList = st.List(between1, between2, _DateBeginSessionFilter, _DateEndSessionFilter, _NumberSessionFilter, _DescriptionSessionFilter, _PlaceSessionFilter, extraWhereConfidential);
			model.SessionList = new StaticPagedList<Session>(model.SessionList, page ?? 1, BLL.PageSize(cfg), pageCount);

			model.CurrentUserid = uId;
			model.ShowReSend = BLL.Cfgs(User.Claims).AutoTransc && BLL.Cfgs(User.Claims).Global_Others_TranscriptionProvider != "DigitroProvider" ? 1 : 0;
			model.DateBeginSessionFilter = _DateBeginSessionFilter;
			model.DateEndSessionFilter = _DateEndSessionFilter;
			model.NumberSessionFilter = NumberSessionFilter;
			model.DescriptionSessionFilter = DescriptionSessionFilter;
			model.PlaceSessionFilter = PlaceSessionFilter;
			model.PlaceFilterListFilter = new List<IdName>() { new IdName() { Id = -1, Name = "Todos" } }.Concat(Place.ListAll(cfg));

			HttpContext.Session.SetString("DateBeginSessionFilter", DateBeginSessionFilter ?? "");
			HttpContext.Session.SetString("DateEndSessionFilter", DateEndSessionFilter ?? "");
			HttpContext.Session.SetString("NumberSessionFilter", NumberSessionFilter ?? "");
			HttpContext.Session.SetString("DescriptionSessionFilter", DescriptionSessionFilter ?? "");
			HttpContext.Session.SetString("PlaceSessionFilter", PlaceSessionFilter ?? "");

			return View(model);
		}

		[Authorize(Roles = "1")]
		public ActionResult Ramifications(long sessionId) {
			Session st = new Session(cfg, User.Claims);
			SessionViewModel model = new SessionViewModel();
			long uId = BLL.CurrentUserId(User.Claims);

			//verifica se o usuario logado possui permissao para acessar sessoes sigilosas
			string extraWhereConfidential = new UserK(uId, cfg).IsConfidentialAllowed() ? "" : " AND NVL(s.Confidential, 0) = 0 ";

			model.SessionList = st.ListRamifications(sessionId, extraWhereConfidential);
			model.CurrentUserid = uId;
			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult AutoTranscStatus(string id) {
			IAutoTranscProvider autoTransc = BLL.AutoTranscProvider(cfg, User.Claims);
			IJobDetails details = autoTransc.GetTranscriptionJobDetails(id);
			string message = "";
			if (details.Status == JobStatus.Queued) {
				message = "Auto-transcrição está em fila. Deseja aguardar?";
			} else if (details.Status == JobStatus.Transcribing) {
				message = "Auto-transcrição está em andamento. Deseja aguardar?";
			}
			//para teste : return Json(new { status = 2, message });
			return Json(new { status = (int)details.Status, message });
		}


		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult IsTranscribedInDesktop(int id) {

			SessionTask st = new SessionTask(cfg, User.Claims, id);

			string message = "";

			return Json(new { status = (int)st.TranscribedInEnum, message });

		}


		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult ResendAutoTransc(long taskId) {
			try {
				if (BLL.GetConfig("Global_Others_TranscriptionProvider", cfg) == "NenhumProvider") return Json(new { status = "Auto-transcrição está desativada." });
				BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, taskId) as BaseTranscriptionTask;
				if (st.Status != FlowStepStatus.Paused) return Json(new { status = "Tarefa deve estar com status Pausada." });
				if (!st.Session.AutoTranscription) return Json(new { status = "Auto-transcrição desabilitada na sessão." });
				if (st.AutoTranscId != null) {
					IAutoTranscProvider autoTransc = BLL.AutoTranscProvider(cfg, User.Claims);
					IJobDetails details = autoTransc.GetTranscriptionJobDetails(st.AutoTranscId);
					if (details.Status == JobStatus.Queued) return Json(new { status = "Auto-transcrição está em fila." });
					else if (details.Status == JobStatus.Transcribed) return Json(new { status = "Auto-transcrição já está concluída." });
					else if (details.Status == JobStatus.Transcribing) return Json(new { status = "Auto-transcrição está sendo processada." });
				}
				CreateJobResult result = st.SendAutoTransc();
				if (result.Status == CreateJobStatus.Disabled) return Json(new { status = "Auto-transcrição está desabilitada." });
				else if (result.Status == CreateJobStatus.Error) return Json(new { status = "Ocorreu erro na auto-transcrição." });
				else if (result.Status == CreateJobStatus.Offline) return Json(new { status = "Serviço de auto-transcrição está desabilitado." });
				else if (result.Status == CreateJobStatus.Success) return Json(new { status = "Auto-transcrição criada com sucesso.", autotranscid = result.Reference });
				else return Json(new { status = "Erro insperado." });
			} catch (Exception ex) {
				return Json(new { status = ex });
			}
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult ExistsTranscription(string taskId) {
			TranscriptionData td = new TranscriptionData(cfg, User.Claims);
			return Json(new { exists = td.HasTranscriptionData(Convert.ToInt64(taskId)) ? 1 : 0 });
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult FileUpload(long taskId, IFormFile file) {
			SessionTask st = new SessionTask(cfg, User.Claims, taskId);
			st.ChangeStatus(FlowStepStatus.ImportedWeb);
			TranscriptionData td = new TranscriptionData(cfg, User.Claims, taskId);
			td.Save(taskId, file);
			return StatusCode((int)HttpStatusCode.OK);
		}
		[HttpPost]
		[Authorize(Roles = "1")]
		public IActionResult NewIndex(PlaySessionViewModel index) {
			int? _AttendantId = index.IndexAttendantId == "-1" ? null : (int?)Convert.ToInt32(index.IndexAttendantId);
			int? _PresidenteId = index.IndexPresidentetId == "-1" ? null : (int?)Convert.ToInt32(index.IndexPresidentetId);
			int? _RelatorId = index.IndexRelatorId == "-1" ? null : (int?)Convert.ToInt32(index.IndexRelatorId);
			int? _ClasseId = index.IndexClasseId == "-1" ? null : (int?)Convert.ToInt32(index.IndexClasseId);
			int? _RecursoId = index.IndexRecursoId == "-1" ? null : (int?)Convert.ToInt32(index.IndexRecursoId);
			int? _MateriaId = index.IndexMateriaId == "-1" ? null : (int?)Convert.ToInt32(index.IndexMateriaId);
			index.IndexOffset = index.IndexOffset.Replace(".", ",");
			var offset = TimeSpan.FromMilliseconds(Convert.ToInt64(Convert.ToDecimal(index.IndexOffset) * 1000));
			SessionIndex sessionIndex = new SessionIndex() {
				SessionId = index.IndexSessionId,
				Offset = offset.Ticks,
				AttendantId = _AttendantId,
				RecordDate = index.IndexRecordDate + TimeSpan.FromTicks(offset.Ticks),
				Subject = index.IndexSubject,
				Stage = index.IndexStage,
				SpeechType = index.IndexSpeechType,
				UserId = 0,
				Confidential = index.IndexConfidential,
				PresidenteId = _PresidenteId,
				RelatorId = _RelatorId,
				ClasseId = _ClasseId,
				RecursoId = _RecursoId,
				MateriaId = _MateriaId,
				ProcessNumber = index.ProcessNumber,
				Observation = index.Observation
			};

			long indexId = SessionIndex.Insert(cfg, User.Claims, sessionIndex);
			return RedirectToAction("PlaySession", "Session", new { sessionId = index.IndexSessionId, indexId = indexId });
		}

		[HttpGet]
		[Authorize(Roles = "1")]
		public IActionResult PlaySession(int sessionId, int indexId = 0) {

			ViewBag.HasVideo = true;
			ViewBag.VideoNotFound = false;
			ViewBag.ShowAttachment = true;
			ViewBag.ViewMessage = "Página não encontrada!";
			ViewBag.RecaptchaKey = BLL.ReCaptchaSiteKey(cfg);
			ViewBag.UseRecaptcha = BLL.UseRecaptcha(cfg);
			var viewResult = View("Message");

			credential = CredentialsHelper.GetCredential();
			userAgent = Request.Headers["User-Agent"].ToString();

			var drs3xViewModel = GetSession(sessionId);
			var SessionAttendantList = SessionAttendant.ListAll(cfg, sessionId);
			drs3xViewModel.IndexAttendantList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(SessionAttendantList);
			drs3xViewModel.IndexPresidenteList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(SessionAttendantList);
			drs3xViewModel.IndexRelatorList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(SessionAttendantList);
			drs3xViewModel.IndexClasseList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(Classe.ListAll(cfg));
			drs3xViewModel.IndexRecursoList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(Recurso.ListAll(cfg));
			drs3xViewModel.IndexMateriaList = new List<IdName>() { new IdName() { Id = -1, Name = "" } }.Concat(Materia.ListAll(cfg));
			drs3xViewModel.IndexSessionId = sessionId;
			drs3xViewModel.IndexRecordDate = drs3xViewModel.StartDate;
			drs3xViewModel.StartIndexId = indexId;

			if (drs3xViewModel != null) {

				var storagePath = drs3xViewModel.StoragePath;

				if (drs3xViewModel.Status == SessionStatus.Waiting) {
					ViewBag.ViewMessage = "A sessão não teve a sua gravação iniciada.";
					return View("Message");
				}

				if (drs3xViewModel.Files == null) {
					ViewBag.ViewMessage = "Esta sessão não possui gravação.";
					return View("Message");
				}

				if (!SetDRS3xVideo(drs3xViewModel, storagePath)) {
					ViewBag.ViewMessage = "Vídeo em processo de conversão. Tente novamente em alguns instantes.";
					return View("Message");
				}

				if (filePath == null) {
					ViewBag.VideoNotFound = true;
					ViewBag.HasVideo = false;
				}

				if (!browsers.Any(userAgent.Contains)) {
					viewResult = View("PlaySession_WM9", drs3xViewModel);
				} else {
					viewResult = View("PlaySession_MP4_Single", drs3xViewModel);
				}
			} else {
				ViewBag.ViewMessage = "Audiência não Localizada!";
				return View("Message");
			}

			return viewResult;
		}

		private bool SetDRS3xVideo(PlaySessionViewModel session, string storagePath) {
			var mediaConverter = BLL.MediaConverter(cfg);
			var files = WebPlayerHelper.GetVideosPath(session, storagePath);
			filePath = files.Count > 0 ? files[0].Url : "";

			if (!System.IO.File.Exists(filePath)) {
				filePath = null;
				return true;
			}

			ImpersonationHelper.Impersonate(credential, () => { mimeType = MimeTypeHelper.GetMimeType(filePath); });

			if (browsers.Any(userAgent.Contains)) {
				return WebPlayerHelper.ConvertVideo(mediaConverter, ref filePath, ref mimeType, session.Guid);
			}
			return true;
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult PlayDRS3xVideo(long id, int indx = 0) {

			var session = GetSession(id);

			if (session == null) return NotFound();

			var extension = string.Empty;
			FileStream stream = null;
			credential = CredentialsHelper.GetCredential();
			userAgent = Request.Headers["User-Agent"].ToString();
			var storagePath = session.StoragePath;

			SetDRS3xVideo(session, storagePath);
			ImpersonationHelper.Impersonate(credential, () => {
				var inUse = false;
				int count = 0;
				while (!inUse) {
					try {
						stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
						extension = mimeType.Contains("mp4") ? "mp4" : "wmv";
						inUse = true;
					} catch (Exception) {
						Thread.Sleep(500);
						inUse = false;
						if (count >= 50) {
							inUse = true;
							throw new Exception("Verifique permissões de acesso ao servidor de arquivos.");
						}
						count++;
					}
				}
			});

			return File(stream, mimeType, $"{session.Guid}_mediafile.{extension}", true);
		}

		private PlaySessionViewModel GetSession(long sessionId) {
			Session session = new Session(sessionId, cfg, User.Claims);
			var drs3xViewModel = new PlaySessionViewModel() {
				Id = session.Id,
				PlaceId = session.PlaceId,
				TypeId = Convert.ToInt64(session.TypeId),
				Guid = session.GuidFromDB.ToString(),
				Number = session.Number,
				Description = session.Description,
				Duration = session.DurationFromDb,
				Files = session.FilesFromDB,
				Turns = session.Turns,
				AutoTranscription = session.AutoTranscription,
				StoragePath = session.GetGlobalPath(true),
				ExpectedDate = session.ExpectedDate,
				StartDate = session.StartDate,
				FinishDate = session.FinishDate,
				Status = session.Status
			};
			drs3xViewModel.Indexes = new List<SessionIndexViewModel>();
			var sessionIndexes = SessionIndex.List(cfg, User.Claims, session.Id);
			foreach (var item in sessionIndexes) {
				drs3xViewModel.Indexes.Add(new SessionIndexViewModel() {
					Id = item.Id,
					SessionId = item.SessionId,
					AttendantId = item.AttendantId,
					Offset = item.Offset,
					Duration = item.Duration,
					Subject = item.Subject,
					RecordDate = item.RecordDate,
					ProcessNumber = item.ProcessNumber,
					PresidenteId = item.PresidenteId,
					RelatorId = item.RelatorId,
					ClasseId = item.ClasseId,
					MateriaId = item.MateriaId,
					Observation = item.Observation,
					Orador = new Attendant() { Name = item.OradorName },
					Presidente = new Attendant() { Name = item.PresidenteName },
					Relator = new Attendant() { Name = item.RelatorName },
					Classe = new Classe() { Name = item.ClasseName },
					Recurso = new Recurso() { Name = item.RecursoName },
					Materia = new Materia() { Name = item.MateriaName }
				});
			}
			drs3xViewModel.Attendants = new List<SessionAttendantViewModel>();
			var sessionAttendants = SessionAttendant.List(cfg, User.Claims, session.Id);
			foreach (var item in sessionAttendants) {
				var attendantRole = new AttendantRoleViewModel() {
					Name = item.AttendantRole.Name,
				};
				var attendant = new AttendantViewModel() {
					Id = item.AttendantId,
					Active = item.Attendant.Active,
					RoleId = item.Attendant.DefaultRoleId,
					Name = item.Attendant.Name,
					ForeignKey = item.Attendant.ForeignKey,
					Role = attendantRole
				};
				drs3xViewModel.Attendants.Add(new SessionAttendantViewModel() {
					Id = item.Id,
					SessionId = item.SessionId,
					RoleId = item.RoleId,
					AttendantId = item.AttendantId,
					Name = item.Name,
					Attendant = attendant,
					Role = attendantRole
				});
			}
			drs3xViewModel.SessionType = new SessionTypeViewModel() {
				Name = session.SessionTypeName
			};
			drs3xViewModel.Place = new PlaceViewModel() {
				Name = session.PlaceName
			};
			return drs3xViewModel;
		}
	}
}
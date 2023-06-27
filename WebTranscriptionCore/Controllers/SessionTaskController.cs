using Kenta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using X.PagedList;

namespace WebTranscriptionCore {
	public class SessionTaskController : Controller {

		private readonly IConfiguration cfg;

		public SessionTaskController(IConfiguration cfg, IWebHostEnvironment env) => this.cfg = cfg;

		[Authorize(Roles = "1")]
		public ActionResult Index(int? page = 1, string DateSessionFilter = null, string TimeSessionFilter = null, string StatusTaskFilter = null, string UserFilter = null) {
			SessionTask st = new SessionTask(cfg, User.Claims);
			SessionTaskViewModel model = new SessionTaskViewModel();
			long uId = BLL.CurrentUserId(User.Claims);

			DateTime? _DateSessionFilter = (DateSessionFilter.Cleanup() == null || DateSessionFilter.Contains("_")) ? null : (DateTime?)DateTime.ParseExact(DateSessionFilter, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			byte _StatusTaskFilter = StatusTaskFilter.Cleanup() == null ? Convert.ToByte("2") : Convert.ToByte(StatusTaskFilter);
			long? _UserFilter = UserFilter.Cleanup() == null ? uId : (UserFilter == "-1" ? null : (long?)Convert.ToInt64(UserFilter));
			string _TimeSessionFilter = (TimeSessionFilter.Cleanup() == null || TimeSessionFilter.Contains("_")) ? null : TimeSessionFilter;

			string extraWhereConfidential = "";
			#if TSE_DF_DEBUG || TSE_DF_RELEASE
			extraWhereConfidential = new UserK(uId, cfg).IsConfidentialAllowed() ? "" : " AND NVL(s.Confidential, 0) = 0 ";
			#endif

			int pageCount = st.ListCount(_DateSessionFilter, _TimeSessionFilter, _StatusTaskFilter, _UserFilter, extraWhereConfidential);
			int between2 = BLL.PageSize(cfg) * page ?? 1;
			int between1 = between2 - BLL.PageSize(cfg) + 1;
			model.SessionTaskList = st.List(between1, between2, _DateSessionFilter, _TimeSessionFilter, _StatusTaskFilter, _UserFilter, extraWhereConfidential);
			model.SessionTaskList = new StaticPagedList<SessionTask>(model.SessionTaskList, page ?? 1, BLL.PageSize(cfg), pageCount);
			model.StatusTaskListFilter = new List<IdName>() {
				new IdName() {Id = 1, Name= "Todos" },
				new IdName() {Id = 2, Name = "Pendentes" },
				new IdName() {Id = 3, Name = "Exceto Concluídas e Canceladas" },
				new IdName() {Id = 4, Name= "Aguardando" },
				new IdName() {Id = 5, Name= "Em execução" },
				new IdName() {Id = 6, Name= "Rejeitadas" },
				new IdName() {Id = 7, Name= "Concluídas" },
				new IdName() {Id = 8, Name= "Canceladas" }
			};

			model.CurrentUserid = uId;
			model.ShowReSend = BLL.Cfgs(User.Claims).AutoTransc && BLL.Cfgs(User.Claims).Global_Others_TranscriptionProvider != "DigitroProvider" ? 1 : 0;
			model.DateSessionFilter = _DateSessionFilter;
			model.TimeSessionFilter = TimeSessionFilter;
			model.StatusTaskFilter = _StatusTaskFilter.ToString();
			model.UserFilter = UserFilter.Cleanup() ?? uId.ToString();
			model.UserFilterListFilter = new List<IdName>() { new IdName() { Id = -1, Name = "Todos" } }.Concat(UserK.ListAll(cfg));
			model.AutoTrancriptionByTask = (BLL.Cfgs(User.Claims).AutoTrancriptionByTask.ToLower() == "true")? "true" : "false";
			if (st.Session!=null) model.SessionAutoTranscription = (st.Session.AutoTranscription.ToString().ToLower() == "true") ? "true" : "false";

			HttpContext.Session.SetString("DateSessionFilter", DateSessionFilter ?? "");
			HttpContext.Session.SetString("TimeSessionFilter", TimeSessionFilter ?? "");
			HttpContext.Session.SetString("StatusTaskFilter", StatusTaskFilter ?? "");
			HttpContext.Session.SetString("UserFilter", UserFilter ?? "");

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult AllowAutoTranscription(long id){

			if (BLL.GetConfig("Global_Others_TranscriptionProvider", cfg) == "NenhumProvider") return Json(new { status = 0, message = "Transcrição automática está desativada." });

			BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, id) as BaseTranscriptionTask;
			if (!st.Session.AutoTranscription && (BLL.Cfgs(User.Claims).AutoTrancriptionByTask.ToLower() == "false")) return Json(new { status = 0, message = "Transcrição automática desabilitada na sessão." });

			if (st.FlowStep.Is<BasePartialTranscriptionTask>() && st.AllowAutoTranscription) return Json(new { status = 1, message = "Transcrição automática habilitada para esta tarefa" });
			else return Json(new { status = 0, message = "Transcrição automática desabilitada para este tipo de tarefa" });
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult AutoTranscStatus(string id) {
			IAutoTranscProvider autoTransc = BLL.AutoTranscProvider(cfg, User.Claims);
			IJobDetails details = autoTransc.GetTranscriptionJobDetails(id);
			string message = "";
			if (details.Status == JobStatus.Queued) {
				message = "Transcrição automática está em fila. Deseja aguardar?";
			} else if (details.Status == JobStatus.Transcribing) {
				message = "Transcrição automática está em andamento. Deseja aguardar?";
			}
			else if (details.Status == JobStatus.NotFound){
				message = "Transcrição automática não foi criada, deseja criar?";
			}
			//para teste :
			//return Json(new { status = 2, message });
			return Json(new { status = (int)details.Status, message });
		}


		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult IsTranscribedInDesktop(int id) {

			SessionTask st = new SessionTask(cfg, User.Claims, id);
			string message = string.Empty;
			var statusTask = -1;
			if (st.TranscribedInEnum == TranscribedInEnum.Desktop){
				message = "Transcrição iniciada no Desktop deseja continuar mesmo podendo ter perdas de formatação?";
				statusTask = (int)st.TranscribedInEnum;
			}
			else {
				message = "Transcrição iniciada na web";
				statusTask = (int)st.TranscribedInEnum;
			}
			return Json(new { status = statusTask, message });
		}

		public ActionResult UpdateAutoTranscriptionStatus(int id) {

			string message = "O status da trasncrição automática foi verificado com sucesso.";
			IAutoTranscProvider autoTransc = BLL.AutoTranscProvider(cfg, User.Claims);
			IJobDetails details = autoTransc.GetTranscriptionJobDetails(id.ToString());

			// Atualiza o status da auto transcrição
			var ath = new AutoTranscriptionHistory(cfg);
			var autoTranscriptionHistory = ath.LoadByTaskId(id);
			if (autoTranscriptionHistory != null) {
				if (details.Status != (JobStatus)autoTranscriptionHistory.Status) {
					ath.UpdateStatus(id, (int)details.Status, details.DateEnd);
					message = "O status da transcrição automática foi atualizado.";
				}
			}

			return Json(new { message });
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult SendAutoTransc(long taskId) {
			try {
				if (BLL.GetConfig("Global_Others_TranscriptionProvider", cfg) == "NenhumProvider") return Json(new { status = "Transcrição automática está desativada." });
				BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, taskId) as BaseTranscriptionTask;
				//if (st.Status != FlowStepStatus.Paused) return Json(new { status = "Tarefa deve estar com status Pausada." });
				if (!st.Session.AutoTranscription && (BLL.Cfgs(User.Claims).AutoTrancriptionByTask.ToLower() == "false")) return Json(new { status = "Transcrição automática desabilitada na sessão." });
				if (st.AutoTranscId != null) {
					IAutoTranscProvider autoTransc = BLL.AutoTranscProvider(cfg, User.Claims);
					IJobDetails details = autoTransc.GetTranscriptionJobDetails(st.AutoTranscId);

					if (details.Status == JobStatus.Queued) return Json(new { status = JobStatus.Queued, message = "Transcrição automática está em fila." });
					else if (details.Status == JobStatus.Transcribing) return Json(new { status = JobStatus.Transcribing, message = "Transcrição automática está em processamento." });
					else if (details.Status == JobStatus.Transcribed) return Json(new { status = JobStatus.Transcribed, message = "Transcrição automática já está concluída." });
					else return Json(new { status = details.Status, message = "Erro insperado. Tente novamente" });

				}

				CreateJobResult result = st.SendAutoTransc();
				switch (result.Status) {
					case CreateJobStatus.Disabled:
						return Json(new { status = CreateJobStatus.Disabled, message = "Transcrição automática está desabilitada." });
					case CreateJobStatus.Error:
						return Json(new { status = CreateJobStatus.Error, message = "Ocorreu erro na Transcrição automática." });
					case CreateJobStatus.Offline:
						return Json(new { status = CreateJobStatus.Offline, message = "Serviço de Transcrição automática está desabilitado." });
					case CreateJobStatus.Success:
						return Json(new { status = CreateJobStatus.Success, message = "Transcrição automática criada com sucesso.", autotranscid = result.Reference }); break;
					default:
						return Json(new { status = 400, message = "Erro insperado." });
				}
			} catch (Exception ex) {
				return Json(new { status = 404, message = ex });
			}
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

		//[HttpPost]
		//[Authorize(Roles = "1")]
		//public ActionResult SendAutoTransc(long taskId){
		//	try {
		//		if (BLL.GetConfig("Global_Others_TranscriptionProvider", cfg) == "NenhumProvider") return Json(new { status = "Transcrição automática está desativada." });
		//		BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, taskId) as BaseTranscriptionTask;
		//		if (st.Status != FlowStepStatus.Active) return Json(new { status = "Tarefa deve estar com status Ativa." });
		//		if (!st.Session.AutoTranscription && (BLL.Cfgs(User.Claims).AutoTrancriptionByTask.ToLower() == "false"))
		//			 return Json(new { status = "Transcrição automática está desabilitada." });

		//		CreateJobResult result = st.SendAutoTransc();
		//		if (result.Status == CreateJobStatus.Disabled) return Json(new { status = "Recurso de transcrição automátca está desabilitada." });
		//		else if (result.Status == CreateJobStatus.Error) return Json(new { status = "Ocorreu erro na transcrição desta tarefa." });
		//		else if (result.Status == CreateJobStatus.Offline) return Json(new { status = "Serviço de transcrição automática está desabilitado." });
		//		else if (result.Status == CreateJobStatus.Success) return Json(new { status = "Tarefa de transcrição autompatica foi criada com sucesso.", autotranscid = result.Reference });
		//		else return Json(new { status = "Ops. Erro insperado no serviço." });
		//	}
		//	catch (Exception ex) { 
		//		return Json(new { status = ex });
		//	}
		//}


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
	}
}
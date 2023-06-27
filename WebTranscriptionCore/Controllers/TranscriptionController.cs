using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.Web;

namespace WebTranscriptionCore {
	public class TranscriptionController : Controller {

		private readonly IConfiguration cfg;

		public TranscriptionController(IConfiguration cfg) => this.cfg = cfg;

		[Authorize(Roles = "1")]
		public ActionResult Index(long taskId) {
			Configurations cfgs = JsonConvert.DeserializeObject<Configurations>(User.Claims.First(x => x.Type == "cfgs").Value);
			BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, taskId) as BaseTranscriptionTask;
			st.Execute();
			TranscriptionViewModel model = new TranscriptionViewModel();
			ViewBag.Title = st.Description;
			model.TaskId = taskId.ToString();
			model.WebText = st.GetText;
			model.AudioFileName = st.Session.AudioFileName.Replace(" ", "%20");

			//Tempo inicial e final do trecho que está sendo transcrição para ser setado no player do html 5 que possui o áudio inteiro.
			ViewBag.PlayerHtml5Begin = st.Offset.TotalSeconds.ToStringGB();
			ViewBag.PlayerHtml5End = (st.Offset.TotalSeconds + st.Duration.TotalSeconds).ToStringGB();

			//Duração do turno que está sendo transcrito.
			ViewBag.TurnDuration =  st.Duration.TotalSeconds.ToStringGB();
			ViewBag.TurnDurationFormated = st.Duration.ToString(@"hh\:mm\:ss");

			//Posição inicial do player, para caso onde o usuário pausou a transcrição, e ao executar novamente vir posicionado nesso tempo que parou.
			ViewBag.CurrentPosition = st.AudioPosition.TotalSeconds.ToStringGB();

			ViewBag.PlayKeyCode = GetKey(cfgs.Editor_PlayPause ?? cfgs.Editor_Global_PlayPause);
			ViewBag.PlayKeyCtrl = HasCtrl(cfgs.Editor_PlayPause ?? cfgs.Editor_Global_PlayPause);

			ViewBag.FasterKeyCode = GetKey(cfgs.Editor_Faster ?? cfgs.Editor_Global_Faster);
			ViewBag.FasterKeyCtrl = HasCtrl(cfgs.Editor_Faster ?? cfgs.Editor_Global_Faster);

			ViewBag.SlowerKeyCode = GetKey(cfgs.Editor_Slower ?? cfgs.Editor_Global_Slower);
			ViewBag.SlowerKeyCtrl = HasCtrl(cfgs.Editor_Slower ?? cfgs.Editor_Global_Slower);

			ViewBag.BackwardKeyCode = GetKey(cfgs.Editor_Backward ?? cfgs.Editor_Global_Backward);
			ViewBag.BackwardKeyCtrl = HasCtrl(cfgs.Editor_Backward ?? cfgs.Editor_Global_Backward);

			ViewBag.ForwardKeyCode = GetKey(cfgs.Editor_Forward ?? cfgs.Editor_Global_Forward);
			ViewBag.ForwardKeyCtrl = HasCtrl(cfgs.Editor_Forward ?? cfgs.Editor_Global_Forward);

			ViewBag.PauseRewind = (cfgs.Editor_PauseRewind ?? cfgs.Editor_Global_PauseRewind).TotalSeconds.ToStringGB();

			ViewBag.PrevTextAllowed = !st.FirstMark;
			ViewBag.NextTextAllowed = !st.LastMark;

			return View(model);
		}

		private int GetKey(string cfg) {
			Enum.TryParse(cfg.Replace("Ctrl +", "").Replace("Alt +", ""), out Keys key);
			return (int)key;
		}

		private int HasCtrl(string key) => key.Contains("Ctrl +") ? 1 : 0;

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult SaveDoc(string id, string text, string plainText) {
			try {
				TranscriptionDataWeb td = new TranscriptionDataWeb(cfg, User.Claims);
				td.Save(Convert.ToInt64(id), text, plainText);
			} catch (ValidationException ex) {
				return Json(new { sucess = false, errorMessage = $"Validação: {ex.Message}" });
			} catch (Exception ex) {
				return Json(new { sucess = false, errorMessage = $"Erro ao salvar documento: {ex}" });
			}
			return Json(new { sucess = true });
		}

		[Authorize(Roles = "1")]
		public ActionResult Pause(long taskId, string progress, string currTime) {
			string[] time = currTime.Split(':');
			string[] timeMiliSecs = time[2].Split('.');
			TimeSpan tsTime = new TimeSpan(0, Convert.ToInt32(time[0]), Convert.ToInt32(time[1]), Convert.ToInt32(timeMiliSecs[0]), Convert.ToInt32(timeMiliSecs[1]));

			SessionTask st = new SessionTask(cfg, User.Claims, taskId);
			st.Pause(Convert.ToDecimal(progress), tsTime);

			return RedirecToSessionTask();
		}

		private ActionResult RedirecToSessionTask() => RedirectToAction("Index", "SessionTask", new {
			page = 1,
			DateSessionFilter = HttpContext.Session.GetString("DateSessionFilter"),
			TimeSessionFilter = HttpContext.Session.GetString("TimeSessionFilter"),
			StatusTaskFilter = HttpContext.Session.GetString("StatusTaskFilter"),
			UserFilter = HttpContext.Session.GetString("UserFilter")
		});

		[Authorize(Roles = "1")]
		public ActionResult Accept(long taskId) {
			SessionTask st = new SessionTask(cfg, User.Claims, taskId);
			st.Accept();
			return RedirecToSessionTask();
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult PrevText(string id) {
			BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, Convert.ToInt64(id)) as BaseTranscriptionTask;
			return Content(st.PrevText);
		}

		[HttpPost]
		[Authorize(Roles = "1")]
		public ActionResult NextText(string id) {
			BaseTranscriptionTask st = SessionTask.GetById(cfg, User.Claims, Convert.ToInt64(id)) as BaseTranscriptionTask;
			return Content(st.NextText);
		}



	}
}
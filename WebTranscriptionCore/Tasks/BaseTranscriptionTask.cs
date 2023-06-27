using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public class BaseTranscriptionTask : SessionTask {

		public virtual bool AllowAutoTranscription => false;

		private TranscriptionDataWeb trDataWeb;

		public BaseTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public BaseTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public BaseTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public BaseTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		public TranscriptionDataWeb TrDataWeb {
			get {
				trDataWeb = trDataWeb ?? new TranscriptionDataWeb(Id, cfg, claims);
				return trDataWeb.TaskId == null ? null : trDataWeb;
			}
		}

		public virtual TimeSpan Offset => TimeSpan.Zero;

		public virtual TimeSpan Duration => Session.Duration;

		public virtual string GetText => TrDataWeb?.Text;

		public virtual string GetPlainText => TrDataWeb?.PlainText;

		public string PrevText => GetPrevOrNextText(true);

		public string NextText => GetPrevOrNextText(false);

		/// </summary>
		/// <returns></returns>
		public override bool BeforeExecute() {
			if (!base.BeforeExecute()) return false;
			//if (TranscribedInEnum == TranscribedInEnum.Desktop){
				//throw new ValidationException("Tarefa iniciada no Word. Não é possível continuá-la no módulo web para que não ocorra perda de formatação do texto.");
			//}
			return true;
		}

		public override void Execute() {
			base.Execute();
			//new TranscriptionDataWeb(cfg, claims).UpdateTranscribedIn(Id, cfg);
		}


		protected virtual string GetPrevOrNextText(bool prev) => "";


		protected void CleanText() => trDataWeb = null;


		public CreateJobResult SendAutoTransc() {
			string input = Session.AudioFileName;
			string output = Path.Combine(WebRootPath, "ffmpeg", Id.ToString() + ".mp3");
			TimeSpan offset = BLL.RecalculateTimeForMp3(Offset, false);
			BLL.SliceFile(input, output, offset.TotalSeconds, Duration.TotalSeconds, claims);
			IAutoTranscProvider at = BLL.AutoTranscProvider(cfg, claims);
			CreateJobResult result = at.CreateTranscriptionJob(Id.Value, output, Duration);
			UpdateAutoTranscId(result.Reference);
			if (TrDataWeb == null){
				this.trDataWeb = (new TranscriptionDataWeb(cfg, claims));
				this.trDataWeb.Save(Id.Value, null, null, false);
			}
			else TrDataWeb.Save(Id.Value, null, null, false);
            return result;
		}
	}
}
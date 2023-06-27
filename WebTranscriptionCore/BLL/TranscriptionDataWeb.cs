using Kenta.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class TranscriptionDataWeb : BaseClass {

		public long? TaskId { get; set; }
		public string Text { get; set; }
		public string PlainText { get; set; }
		public TranscribedInEnum TranscribedIn { get; set; }

		public TranscriptionDataWeb() { }

		public TranscriptionDataWeb(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) {		}

		public TranscriptionDataWeb(long? taskId, IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) {
			if (taskId == null) return;
			TranscriptionDataWeb transcriptionDataWeb = BLL.Cnn(cfg).QueryFirst<TranscriptionDataWeb>("SELECT TRANSCRIBEDIN, TASKID, TEXT, PLAINTEXT FROM TranscriptionDataWeb WHERE taskId = :taskId", new { taskId });
			if (transcriptionDataWeb == null) return;
			if (transcriptionDataWeb.TranscribedIn == TranscribedInEnum.Desktop) {
				transcriptionDataWeb = BLL.Cnn(cfg).QueryFirst<TranscriptionDataWeb>("SELECT TASKID, PLAINTEXT FROM TranscriptionData WHERE taskId = :taskId", new { taskId });
				TaskId = transcriptionDataWeb?.TaskId;
				Text = transcriptionDataWeb?.PlainText?.Replace("\r", "<br>");
				PlainText = transcriptionDataWeb?.PlainText;
				return;
			};
			TaskId = transcriptionDataWeb.TaskId;
			Text = transcriptionDataWeb.Text;
			PlainText = transcriptionDataWeb.PlainText;
		}

		private bool HasTranscriptionDataWeb(long? taskId) {
			return BLL.Cnn(cfg).ExecScalar<short?>("SELECT 1 FROM TranscriptionDataWeb WHERE TaskId = :taskId", new { taskId }) == 1;
		}

		public void Save(long taskId, string text, string plainText, bool checkStatus = true) {
			SessionTask st = new SessionTask(cfg, claims, taskId);
			if (checkStatus && (st.Status != FlowStepStatus.Executing || st.AssignedUserId != uId))
				throw new ValidationException("Tarefa indisponível para salvar.");
			if (HasTranscriptionDataWeb(taskId))
				BLL.Cnn(cfg).UpdateSQL<TranscriptionDataWeb>(new { transcribedIn = (int)TranscribedInEnum.Web , text = text.ToClob(), plainText = plainText.ToClob(), Date = DateTime.Now, UserId = uId, taskId }, "taskId");
			else
				BLL.Cnn(cfg).InsertSQL<TranscriptionDataWeb>(new { taskId, TranscribedIn = (int)TranscribedInEnum.Web, text = text.ToClob(), plainText = plainText.ToClob(), Date = DateTime.Now, UserId = uId });
			new TranscriptionHistoryWeb(cfg, uId).Save(taskId, text, plainText);
		}

		public void UpdateTranscribedIn(long? taskId, IConfiguration cfg) {
			TranscribedInEnum transcribedIn = TranscribedInEnum.Web;
			if (HasTranscriptionDataWeb(taskId))
				BLL.Cnn(cfg).UpdateSQL<TranscriptionDataWeb>(new { TranscribedIn = (int)transcribedIn, taskId }, "taskId");
			else
				BLL.Cnn(cfg).InsertSQL<TranscriptionDataWeb>(new { TranscribedIn = (int)transcribedIn, taskId });
		}
	}
}
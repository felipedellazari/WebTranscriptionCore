using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class MergingTask : BaseTranscriptionTask {

		public MergingTask(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public MergingTask(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public MergingTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public MergingTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		public override bool BeforeExecute() {
			if (! base.BeforeExecute()) return false;
			if (GetText == null) {
				if (StepIdPrior == null) throw new ValidationException("Etapa anterior não encontrada.");
				string sql = @"SELECT	1 
									FROM		SessionTask st 
												INNER JOIN FlowStep fs ON fs.Id = st.StepId 
												INNER JOIN TranscriptionDataWeb tdw ON tdw.TaskId = st.Id
									WHERE		st.SessionId = :sessionId AND 
												st.StepId = :StepIdPrior AND		
												(tdw.TranscribedIn IS NULL OR tdw.TranscribedIn <> " + (int)TranscribedInEnum.Web + ")";
				if (BLL.Cnn(cfg).ExecScalar<int?>(sql, new { sessionId, StepIdPrior }) == 1)
					throw new ValidationException("Junção não permitida pois transcrição parcial foi executada no ambiente desktop. Execute a junção no módulo desktop.");
			}
			return true;
		}

		public override void Execute() {
			base.Execute();
			if (GetText == null) {
				string sql = "SELECT tdw.TEXT, tdw.PLAINTEXT FROM SessionTask st INNER JOIN TranscriptionDataWeb tdw ON tdw.TaskId = st.Id WHERE st.Sessionid = :sessionId AND st.StepId = :stepId ORDER BY st.Mark";
				IEnumerable<SessionTask> lst = BLL.Cnn(cfg).Query<SessionTask>(sql, new { sessionId, stepId = StepIdPrior });
				string text =  string.Join(Environment.NewLine, lst.Select(x => x.Text).ToArray());
				string plaintText = string.Join(Environment.NewLine, lst.Select(x => x.PlainText).ToArray());
				new TranscriptionDataWeb(cfg, claims).Save(Id.Value, text, plaintText);
				CleanText();
			}
		}
	}
}
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class PartialRevisionTask : BasePartialTranscriptionTask {

		public PartialRevisionTask(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public PartialRevisionTask(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public PartialRevisionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public PartialRevisionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		public override void Execute() {
			base.Execute();
			if (GetText == null) {
				BaseTranscriptionTask stPrev = new BaseTranscriptionTask(cfg, claims, sessionId, StepIdPrior.Value, Mark);
				new TranscriptionDataWeb(cfg, claims).Save(Id.Value, stPrev.GetText, stPrev.GetPlainText);
				CleanText();
			}
		}
	}
}
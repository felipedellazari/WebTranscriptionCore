using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class Transcription2Task : BaseTranscriptionTask {

		public Transcription2Task(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public Transcription2Task(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public Transcription2Task(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public Transcription2Task(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		public override void Execute() {
			base.Execute();
			if (GetText == null && Session.AutoTranscription && AutoTranscId != null) {
				IAutoTranscProvider autoTransc = BLL.AutoTranscProvider(cfg, claims);
				IJobDetails details = autoTransc.GetTranscriptionJobDetails(AutoTranscId);
				if (details.Status == JobStatus.Transcribed) {
					new TranscriptionDataWeb(cfg, claims).Save(Id.Value, details.Transcription, details.Transcription);
					CleanText();
				}
			}
		}
	}
}
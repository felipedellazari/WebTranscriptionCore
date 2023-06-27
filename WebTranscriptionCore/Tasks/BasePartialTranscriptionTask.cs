using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public abstract class BasePartialTranscriptionTask : BaseTranscriptionTask {

		public int TurnIndex => Convert.ToInt32(Arguments);
		public Turn Turn => Session.Turns.Length > 0 && int.TryParse(Arguments, out _) ? Session.Turns[Convert.ToInt32(Arguments)] : null;

		public override TimeSpan Offset {
			get {
				TimeSpan extraTime = TimeSpan.Parse(BLL.GetConfig("Schedule_ExtraTime", cfg));
				TimeSpan o = Session.Turns[TurnIndex].FileOffsetTS.Value;
				if (o < extraTime) return TimeSpan.Zero;
				return o - extraTime;
			}
		}

		public override TimeSpan Duration {
			get {
				TimeSpan o = Session.Turns[TurnIndex].FileOffsetTS.Value;
				TimeSpan d = Session.Turns[TurnIndex].FileDurationTS.Value;
				TimeSpan e = TimeSpan.Parse(BLL.GetConfig("Schedule_ExtraTime", cfg));
				TimeSpan sd = Session.Duration;
				if (sd == TimeSpan.Zero || sd > (o + d)) d += e;
				d += TimeSpan.FromTicks(Math.Min(e.Ticks, o.Ticks));
				return d;
			}
		}

		public BasePartialTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public BasePartialTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public BasePartialTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public BasePartialTranscriptionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		protected override string GetPrevOrNextText(bool prev) {
			long mark = Mark + (prev ? -1 : 1);
			string text = "";

			//busca a última etapa do tipo BasePartialTranscriptionTask.
			FlowStep step = FlowType.LastStep<BasePartialTranscriptionTask>();
			if (step == null) return "";

			//busca o texto no step. Se não encontrar, procura nos steps anteriores.
			while (step.Is<BasePartialTranscriptionTask>()) {
				text = (Session.Tasks.FirstOrDefault(x => x.FlowStep.Id == step.Id && x.Mark == mark) as BasePartialTranscriptionTask).GetText;
				if (text != null) break;
				step = step.Prior;
			}

			return text;
		}
	}
}
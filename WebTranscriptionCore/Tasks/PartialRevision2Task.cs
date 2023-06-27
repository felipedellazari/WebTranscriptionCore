using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class PartialRevision2Task : BaseTranscriptionTask {

		public PartialRevision2Task(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public PartialRevision2Task(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public PartialRevision2Task(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public PartialRevision2Task(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		public override bool BeforeExecute() {
			if (!base.BeforeExecute()) return false;
			if (GetTranscripitons().Any(x => x.Status != FlowStepStatus.Done))
			{
				throw new ValidationException("Existem transcrições que ainda não foram encerradas");
			}	
			if (GetText == null)
			{
				if (GetTranscripitons().Any(x => x.TranscribedInEnum != TranscribedInEnum.Web))
				{
					throw new ValidationException("Revisão Parcial não permitida pois a transcrição parcial foi executada no ambiente desktop. Execute a Revisão Parcial no módulo desktop.");
				}
			}
			return true;
		}

		private IEnumerable<BasePartialTranscriptionTask> GetTranscripitons() {
			FlowStep step = FlowType.LastStep<BasePartialTranscriptionTask>();
			long[] turns = JsonConvert.DeserializeObject<long[]>(Arguments);
			List<BasePartialTranscriptionTask> tasks = new List<BasePartialTranscriptionTask>();
			foreach (SessionTask item in Session.Tasks)
				if (item is BasePartialTranscriptionTask tt && tt.FlowStep.Id == step.Id && turns.Contains(tt.TurnIndex))
					tasks.Add(tt);
			return tasks.OrderBy(x => x.Mark);
		}

		public override void Execute() {
			base.Execute();
			if (GetText == null) {
				IEnumerable<BasePartialTranscriptionTask> tasks = GetTranscripitons();
				string text = string.Join(Environment.NewLine, tasks.Select(x => x.GetText).ToArray());
				string plaintText = string.Join(Environment.NewLine, tasks.Select(x => x.GetPlainText).ToArray());
				new TranscriptionDataWeb(cfg, claims).Save(Id.Value, text, plaintText);
				CleanText();
			}
		}

		protected override string GetPrevOrNextText(bool prev) {
			long mark = Mark + (prev ? -1 : 1);
			return (Session.Tasks.FirstOrDefault(x => x.FlowStep.Id == stepId && x.Mark == mark) as BaseTranscriptionTask).GetText;
		}
	}
}
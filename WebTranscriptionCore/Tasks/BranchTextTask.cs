using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public class BranchTextTask : BaseTranscriptionTask {

		public BranchTextTask(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public BranchTextTask(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) { }

		public BranchTextTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) { }

		public BranchTextTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) { }

		private BaseTranscriptionTask TaskParent {
			get {
				FlowStep step = Session.Parent.FlowType.LastStep<BaseTranscriptionTask>();
				foreach (BaseTranscriptionTask st in Session.Parent.Tasks) {
					//Retorna a primeira que encontrar, pois assume que a última tarefa é do tipo MergingTask, e que só terá uma tarefa desse tipo.
					if (st.FlowStep.Id == step.Id)
						return st;
				}
				return null;
			}
		}

		public override bool BeforeExecute() {
			if (!base.BeforeExecute()) return false;
			if (GetText == null) {
				Session s = Session.Parent;
				if (s == null) throw new ValidationException("Não há sessão pai");
				if (s.Status != SessionStatus.Complete) throw new ValidationException("A sessão pai ainda não está completa");
				if (TaskParent.TranscribedInEnum != TranscribedInEnum.Web) throw new ValidationException("Não é possível executar essa tarefa, pois seu texto base foi feito no Word.");
			}
			return true;
		}

		public override void Execute() {
			base.Execute();
			Session s = Session.Parent;
			if (GetText == null) {
				new TranscriptionDataWeb(cfg, claims).Save(Id.Value, TaskParent.GetText, TaskParent.GetPlainText);
				CleanText();
			}
		}
	}
}
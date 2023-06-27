using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace WebTranscriptionCore {
	public class FlowType : BaseClass {

		private List<FlowStep> steps;
		private List<FlowStepJoin> stepsJoin;

		public long Id { get; set; }

		public FlowType(){ }

		public IEnumerable<FlowStep> Steps {
			get {
				if (steps == null) {
					string sql = @"SELECT	ID, MULTIPLE, CLASSNAME, TYPEID
										FROM		FlowStep 
										WHERE		TypeId = :Id";
					steps = new List<FlowStep>();

					IEnumerable<FlowStep> lst = BLL.Cnn(cfg).Query<FlowStep>(sql, new { Id });

					foreach (FlowStep item in lst)
						steps.Add(new FlowStep(cfg, item, this));
				}
				return steps;
			}
		}

		public IEnumerable<FlowStepJoin> StepsJoin {
			get {
				if (stepsJoin == null) {
					string sql = @"SELECT	PRIORID, NEXTID
										FROM     FlowStepJoinT fsj 
														INNER JOIN FlowStep fs ON fs.ID = fsj.PriorId 
										WHERE		fs.TypeId = :Id";
					stepsJoin = new List<FlowStepJoin>();

					IEnumerable<FlowStepJoinIds> lst = BLL.Cnn(cfg).Query<FlowStepJoinIds>(sql, new { Id });

					foreach (FlowStepJoinIds item in lst)
						stepsJoin.Add(new FlowStepJoin() {
							Prior = Steps.FirstOrDefault(x => x.Id == item.PriorId),
							Next = Steps.FirstOrDefault(x => x.Id == item.NextId)
						});
				}
				return stepsJoin;
			}
		}

		public FlowType(IConfiguration cfg, long id) : base(cfg) => Id = id;

		public FlowStep LastStep<T>() {
			FlowStep step = null;
			foreach (FlowStep s in Steps) {
				if (s.Is<T>() && (s.Next == null || !s.Next.Is<T>())) {
					step = s;
					break;
				}
			}
			return step;
		}

		public static FlowType GetById(IConfiguration cfg, long id) {
			string sql = @"SELECT ID FROM ""FLOWTYPE"" WHERE ID = :id";

			FlowType flowType = BLL.Cnn(cfg).QueryFirst<FlowType>(sql, new { id });

			if (flowType == null) return null;
			FlowType ft = new FlowType(cfg, flowType.Id);
			return ft;
		}

	}
}
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace WebTranscriptionCore {

	public class FlowStep : BaseClass {

		private FlowType flowType;

		public long Id { get; set; }

		public bool Multiple { get; set; }

		public Type InstanceType { get; protected set; }

		public string ClassName { get; set; }

		public string ClassNameFull { get; set; }

		public string AssembyName { get; set; }

		public bool IsCustomization { get; set; }

		public FlowStep() { }
		public FlowStep Prior => flowType.StepsJoin.FirstOrDefault(x => x.Next.Id == Id)?.Prior;

		public FlowStep Next => flowType.StepsJoin.FirstOrDefault(x => x.Prior.Id == Id)?.Next;

		public FlowStep(IConfiguration cfg, FlowStep flowStep, FlowType flowType) : base(cfg) {
			Id = flowStep.Id;
			Multiple = BLL.ToBool(flowStep.Multiple);
			string[] fullName = flowStep.ClassName.Split(",");
			ClassNameFull = fullName[0];
			string[] classArray = ClassNameFull.Split(".");
			ClassName = classArray[classArray.Length - 1];
			AssembyName = fullName[1];
			InstanceType = Type.GetType("WebTranscriptionCore." + ClassName);
			IsCustomization = flowStep.ClassName.EndsWith(".Customization");
			this.flowType = flowType;
		}

		public bool Is<T>() {
			Type t = typeof(T);
			if (InstanceType == null) return false;
			return (InstanceType == t) || InstanceType.IsSubclassOf(t);
		}
	}
}
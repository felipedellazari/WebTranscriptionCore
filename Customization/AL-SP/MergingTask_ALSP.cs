using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using WebTranscriptionCore;

namespace Kenta.DRSPlenario
{
	public class MergingTask_ALSP : MergingTask
	{
		public MergingTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims)
		{
		}

		public MergingTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id)
		{
		}

		public MergingTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId)
		{
		}

		public MergingTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark)
		{
		}

		public bool GetExtrasFiles(out List<MergingFile> files)
		{
			files = new List<MergingFile>();
			//OpenFileDialog dlg = new OpenFileDialog();
			//dlg.Title = "Selecione o arquivo do ementário";
			//dlg.Filter = "Microsoft Word (*.docx)|*.docx";
			//if (dlg.ShowDialog(MainForm.Instance).Equals(DialogResult.Cancel)) return false;
			//files.Add(new MergingFile(dlg.FileName, "Ementário", BLL.Users.Current.Name, BLL.Users.Current.Initials, false));
			return true;
		}

		/*
		public bool CanReopen
		{
			get
			{
				// 1) Não pode ter próximas tarefas com status diferente de Waiting e Active
				// 2) Tarefa atual precisa ser Done
				return !Next.Any(t => !t.Status.In(FlowStepStatus.Waiting, FlowStepStatus.Active)) && (Status == FlowStepStatus.Done);
			}
		}
		*/

	}

	

}
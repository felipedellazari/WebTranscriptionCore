using WebTranscriptionCore;

namespace Kenta.DRSPlenario
{
	public class MergingFile
	{
		public string FileName { get; private set; }
		public string Text { get; private set; }
		public string User { get; private set; }
		public string Initials { get; private set; }
		public bool Delete { get; private set; }
		public long? TaskId { get; private set; }

		public MergingFile(string fileName, string text, string user, string initials, bool delete = true)
		{
			FileName = fileName;
			Text = text;
			User = user;
			Initials = initials;
			Delete = delete;
		}
		public MergingFile(string fileName, string text, string user, string initials, bool delete, long? taskId)
		{
			FileName = fileName;
			Text = text;
			User = user;
			Initials = initials;
			Delete = delete;
			TaskId = taskId;
		}
	}
}

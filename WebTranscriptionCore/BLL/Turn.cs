using System;

namespace WebTranscriptionCore {
	public class Turn {

		public int UsersIdx { get; set; }
		public DateTime Date { get; set; }
		public long Duration { get; set; }

		public TimeSpan DurationTS { get { return TimeSpan.FromTicks(Duration); } }

		public int Status { get; set; }
		public long? FileOffset { get; set; }
		public TimeSpan? FileOffsetTS { get { return FileOffset == null ? null : (TimeSpan?)TimeSpan.FromTicks(FileOffset.Value); } }

		public long? FileDuration { get; set; }
		public TimeSpan? FileDurationTS { get { return FileDuration == null ? null : (TimeSpan?)TimeSpan.FromTicks(FileDuration.Value); } }


	}
}
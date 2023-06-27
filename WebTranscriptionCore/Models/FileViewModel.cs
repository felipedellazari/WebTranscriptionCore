using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class FileViewModel {
		public string DisplayName { get; set; }
		public string FileName { get; set; }
		public string Url { get; set; }
		public TimeSpan Duration { get; set; }
	}
}
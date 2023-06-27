using System;
using System.IO;
using System.Linq;

namespace WebTranscriptionCore {

	public static class MimeTypeHelper {
		private static readonly byte[] MP4_AUDIO = { 0, 0, 0, 28, 102, 116, 121, 112, 105, 115, 111, 109 };
		private static readonly byte[] MP4_VIDEO = { 0, 0, 0, 24, 102, 116, 121, 112, 109, 112, 52, 50 };
		private static readonly byte[] FFMP4_VIDEO = { 0, 0, 0, 32, 102, 116, 121, 112, 105, 115, 111, 109 };
		private static readonly byte[] WMV_WMA = { 48, 38, 178, 117, 142, 102, 207, 17, 166, 217, 0, 170, 0, 98, 206, 108 };

		public static string GetMimeType(string file) {
			var mime = "";

			using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				mime = GetMimeType(stream);
			}

			return mime;
		}

		public static string GetMimeType(FileStream stream) {
			var mime = "application/octet-stream";

			var bytes = new byte[256];
			stream.Read(bytes);

			if (bytes.Take(12).SequenceEqual(MP4_AUDIO)) mime = "audio/mp4";
			else if (bytes.Take(12).SequenceEqual(MP4_VIDEO)) mime = "video/mp4";
			else if (bytes.Take(12).SequenceEqual(FFMP4_VIDEO)) mime = "video/mp4";
			else if (bytes.Take(16).SequenceEqual(WMV_WMA)) mime = "video/x-ms-wmv";

			return mime;
		}
	}
}
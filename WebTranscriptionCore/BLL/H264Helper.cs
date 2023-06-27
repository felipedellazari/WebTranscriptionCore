using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WebTranscriptionCore {

	public static class H264Helper {
		private static readonly string FFMPEG = $@"{Environment.CurrentDirectory}\AppData\Resources\ffmpeg\ffmpeg.exe";
		private static readonly string FFPROBE = $@"{Environment.CurrentDirectory}\AppData\Resources\ffmpeg\ffprobe.exe";
		private static readonly string TEMP_EXT = ".tmp.mp4";
		private static readonly string H264_EXT = ".mp4";
		private static readonly string CONV_ARG = @"-i ""{0}"" -c:v libx264 {1} -r 30 -c:a aac -b:a 32k -f mp4 ""{2}""  -movflags +faststart";

		private static FileInfo current = null;

		private static void Process_Exited(object sender, EventArgs e) {
			//if (current == null) return;

			//var tmp = Path.ChangeExtension($@"{CacheHelper.Path}\{current.Name}", TEMP_EXT);
			//var dst = Path.ChangeExtension($@"{CacheHelper.Path}\{current.Name}", H264_EXT);

			//current = null;
			//File.Move(tmp, dst);
		}

		private static void KillActiveProcess() {
			try {
				var processes = Process.GetProcessesByName("ffmpeg").ToArray();

				for (int i = 0; i < processes.Length; i++) processes[i].Kill();
			} finally {

			}
		}

		private static bool IsAudioOnly(string source) {
			string[] output;

			using (var proc = new Process()) {
				proc.StartInfo = new ProcessStartInfo {
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					FileName = FFPROBE,
					Arguments = $"-loglevel error -show_entries stream=codec_type -of default=nw=1 {source}"
				};
				proc.Start();

				output = proc.StandardOutput.ReadToEnd().TrimEnd().Split("\r\n");
			}

			return !output.Contains("codec_type=video");
		}

		public static void Convert(string source, string dest, string guid) {
			current = new FileInfo(source);
			var rate = "";
			var temp = Path.ChangeExtension($@"{CacheHelper.Path}\{guid}\{current.Name}", TEMP_EXT);

			if (!File.Exists(FFMPEG)) throw new InvalidOperationException($"Caminho não encontrado: {FFMPEG}");

			KillActiveProcess();

			if (!IsAudioOnly(source)) {
				var sz = MediaHelper.GetVideoSize(source).Value;
				rate = (sz.Width == 320) ? "-minrate:v 100k -b:v 100k -maxrate:v 100k" : "-minrate:v 500k -b:v 500k -maxrate:v 500k";
			}

			using (var proc = new Process()) {
				proc.StartInfo = new ProcessStartInfo(FFMPEG) {
					Arguments = string.Format(CONV_ARG, source, rate, temp),
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true
				};
				proc.EnableRaisingEvents = true;
				proc.Exited += Process_Exited;
				proc.Start();
				proc.WaitForExit();
				if (proc.ExitCode != 0) throw new Exception($"Conversion errorCode {proc.ExitCode}");
				if (File.Exists(temp)) File.Move(temp, dest);
			}
		}
	}
}
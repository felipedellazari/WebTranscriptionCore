using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public class BLL {

		public const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);

		public static ConnectionManager Cnn(IConfiguration cfg) => new ConnectionManager(
			cfg.GetSection("DatabaseConnection").GetSection("Provider").Value,
			cfg.GetSection("DatabaseConnection").GetSection("ConnectionString").Value);

		public static IAutoTranscProvider AutoTranscProvider(IConfiguration cfg, IEnumerable<Claim> claims) {
			Configurations cfgs = JsonConvert.DeserializeObject<Configurations>(claims.First(x => x.Type == "cfgs").Value);
			Type t = Type.GetType("WebTranscriptionCore." + cfgs.Global_Others_TranscriptionProvider);
			return (IAutoTranscProvider)Activator.CreateInstance(t, new object[] { cfg, claims });
		}

		public static string GetConfig(string key, IConfiguration cfg) => Cnn(cfg).ExecScalar<string>("SELECT VALUE FROM SystemConfiguration WHERE Name = :key", new { key });

		public static int PageSize(IConfiguration cfg) => Convert.ToInt32(cfg.GetSection("PageSize").Value);

		public static string ReCaptchaSiteKey(IConfiguration cfg) => cfg.GetSection("ReCaptchaSiteKey").Value;

		public static bool UseRecaptcha(IConfiguration cfg) => Convert.ToBoolean(cfg.GetSection("UseRecaptcha").Value);

		public static int MediaConverter(IConfiguration cfg) => Convert.ToInt32(cfg.GetSection("MediaConverter").Value);

		public static bool ToBool(dynamic val) => val is bool ? val : val == 1;

		public static void Obliterate(string path) {
			try {
				if (File.Exists(path)) File.Delete(path);
			} catch (IOException ex) {
				if (Marshal.GetHRForException(ex) == ERROR_SHARING_VIOLATION) {
					Processes.WhoIsLocking(path).ForEach(p => p.Kill());
					File.Delete(path);
				}
			}
		}

		public static void SliceFile(string input, string output, double secsIni, double dur, IEnumerable<Claim> claims) {
			Obliterate(output);
			ProcessStartInfo psi = new ProcessStartInfo(FFmpeg(claims));
			psi.Arguments = "-i " + input + " -ss " + (int)secsIni + " -t " + (int)dur + " " + output;
			using Process proc = new Process();
			proc.StartInfo = psi;
			proc.Start();
			proc.WaitForExit((int)new TimeSpan(0, 10, 0).TotalMilliseconds);
		}

		public static Configurations Cfgs(IEnumerable<Claim> claims) => JsonConvert.DeserializeObject<Configurations>(claims.First(x => x.Type == "cfgs").Value);

		public static long CurrentUserId(IEnumerable<Claim> claims) => Convert.ToInt64(claims.First(x => x.Type == "userId").Value);

		public static string FFmpeg(IEnumerable<Claim> claims) => Path.Combine(claims.First(x => x.Type == "WebRootPath").Value, "ffmpeg", "ffmpeg.exe");

		public static TimeSpan RecalculateTimeForMp3(TimeSpan offset, bool minus = true) {
			double diffMS = offset.TotalMinutes * 137;
			TimeSpan diff = TimeSpan.FromMilliseconds(diffMS);
			//if (minus)
			//	offset = TimeSpan.FromTicks(offset.Ticks - diff.Ticks);
			//else
			return offset = TimeSpan.FromTicks(offset.Ticks + diff.Ticks);
			//return offset;
		}
	}

	public class IdName {
		public long Id { get; set; }
		public string Name { get; set; }
	}
}
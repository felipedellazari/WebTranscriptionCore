using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace WebTranscriptionCore {

	public static class CacheHelper {
		private static readonly IConfiguration config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
		public static string Path => GetCachePath();

		public static void Manage(int cacheDuration, int cacheLimit) {
			var date = DateTime.Today.AddDays(-cacheDuration);
			var drive = DriveInfo.GetDrives().Where(x => x.Name.StartsWith(Path[0]) && x.IsReady).FirstOrDefault();

			if (!Directory.Exists(Path)) return;

			bool.TryParse(config.GetSection("Mp4VideoCache:UseCustom").Value, out bool useCustom);
			if (!useCustom) {
				var limit = (drive.AvailableFreeSpace / (float)drive.TotalSize) * 100;
				if (limit > cacheLimit) return;
			}

			foreach (var fname in Directory.EnumerateFiles(Path, "*.mp4")) {
				if (File.GetCreationTime(fname) < date) File.Delete(fname);
			}
		}

		private static string GetCachePath() {
			bool.TryParse(config.GetSection("Mp4VideoCache:UseCustom").Value, out bool useCustom);

			return useCustom ? config.GetSection("Mp4VideoCache:Storage").Value : $@"{Environment.CurrentDirectory}\AppData\Mp4VideoCache";
		}
	}
}
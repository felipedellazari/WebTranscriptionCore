using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading;

namespace WebTranscriptionCore {

	public static class WebPlayerHelper {
		private static readonly string[] MP4_MimeType = { "audio/mp4", "video/mp4" };

		public static List<FileViewModel> GetVideosPath(PlaySessionViewModel viewModel, string storagePath) {
			var result = new List<FileViewModel>();
			if (viewModel.Files == null) {
				return result;
			}
			dynamic files = JsonConvert.DeserializeObject(viewModel.Files);
			foreach (var item in files) {
				result.Add(new FileViewModel() {
					Url = $"{storagePath}{item.FileName}",
					Duration = new TimeSpan(item.Duration.Value)
				});
			}
			if (result.Count > 1) {
				result.RemoveAll(x => x.Url.ToUpper().Contains(".MP3"));
			}

			return result;
		}

		public static bool ConvertVideo(int mediaConverter, ref string inputFile, ref string mimeType, string guid) {
			inputFile = (mediaConverter == 0) ? GetH264Video(inputFile, mimeType, guid) : GetMp4Video(inputFile, mimeType, guid);
			try {
				mimeType = MimeTypeHelper.GetMimeType(inputFile);
			} catch (IOException) {
				return false;
			}

			return true;
		}

		private static string GetH264Video(string inputFile, string mimeType, string guid) {
			var outputFile = inputFile;

			if (!MP4_MimeType.Contains(mimeType)) {
				outputFile = $@"{CacheHelper.Path}\{guid}\{Path.GetFileNameWithoutExtension(inputFile)}.mp4";

				if (!Directory.Exists($@"{CacheHelper.Path}\{guid}")) Directory.CreateDirectory($@"{CacheHelper.Path}\{guid}");
				if (!File.Exists(outputFile)) H264Helper.Convert(inputFile, outputFile, guid);
			}

			return outputFile;
		}

		private static string GetMp4Video(string inputFile, string mimeType, string guid) {
			var outputFile = inputFile;

			if (!MP4_MimeType.Contains(mimeType)) {
				outputFile = $@"{CacheHelper.Path}\{guid}\{Path.GetFileNameWithoutExtension(inputFile)}.mp4";

				if (!Directory.Exists($@"{CacheHelper.Path}\{guid}")) Directory.CreateDirectory($@"{CacheHelper.Path}\{guid}");
				if (!File.Exists(outputFile)) {
					using (var mutex = new AutoResetEvent(false)) {
						MediaHelper.ConvertToMP4(inputFile, outputFile, () => { mutex.Set(); });
						mutex.WaitOne();
					}
				}
			}

			return outputFile;
		}
	}
}
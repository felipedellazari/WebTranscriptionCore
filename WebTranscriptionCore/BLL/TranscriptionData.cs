using Kenta.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class TranscriptionData : BaseClass {

		public long? TaskId { get; set; }
		public long CRC64 { get; set; }
		public long? UserId { get; set; }
		public byte[] Data { get; set; }
		public string PlainText { get; set; }

		public TranscriptionData(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public TranscriptionData(IConfiguration cfg, IEnumerable<Claim> claims, long taskId) : base(cfg, claims) {
			TaskId = taskId;
			TranscriptionData transcriptionData = BLL.Cnn(cfg).QueryFirst<TranscriptionData>(@"SELECT TASKID, CRC64, USERID, DATA, PLAINTEXT FROM TranscriptionData WHERE TaskId = :taskId", new { taskId });
			if (transcriptionData == null) return;
			CRC64 = (long)transcriptionData.CRC64;
			UserId = (long?)transcriptionData.UserId;
			Data = transcriptionData.Data;
			PlainText = transcriptionData.PlainText;
		}

		public bool HasTranscriptionData(long? taskId) => BLL.Cnn(cfg).ExecScalar<short?>("SELECT 1 FROM TranscriptionData WHERE TaskId = :taskId", new { taskId }) == 1;

		public void Save(long taskId, IFormFile file) {
			using Stream stream = file.OpenReadStream();
			byte[] data = ReadToEnd(stream);
			string plainText = "Importado da web.";
			object args = new {
				taskId,
				Date = DateTime.Now,
				crc64 = unchecked((long)SecurityUtils.CRC64(data)),
				UserId = uId,
				Data = new Blob(data),
				plainText
			};
			if (HasTranscriptionData(taskId))
				BLL.Cnn(cfg).UpdateSQL<TranscriptionData>(args, "taskId");
			else
				BLL.Cnn(cfg).InsertSQL<TranscriptionData>(args);
			new TranscriptionHistory(cfg, claims).CreateHist(taskId, CRC64, UserId, new Blob(Data ?? data), new Clob(PlainText ?? plainText));
		}

		private byte[] ReadToEnd(Stream sourceStream) {
			using var memoryStream = new MemoryStream();
			sourceStream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}

	}
}
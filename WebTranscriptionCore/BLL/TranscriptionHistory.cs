using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class TranscriptionHistory : BaseClass {

		public TranscriptionHistory(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public void CreateHist(long taskId, long crc64, long? userId, Blob data, Clob plainText) {
			BLL.Cnn(cfg).InsertSQL<TranscriptionHistory>(new { taskId, Date = DateTime.Now, crc64, userId, data, plainText });
		}
	}
}
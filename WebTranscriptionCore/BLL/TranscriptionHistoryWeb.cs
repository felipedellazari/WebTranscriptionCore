using Microsoft.Extensions.Configuration;
using System;

namespace WebTranscriptionCore {
	public class TranscriptionHistoryWeb {

		private readonly IConfiguration cfg;
		private readonly long uId;


		public TranscriptionHistoryWeb(IConfiguration _cfg, long _uId) {
			cfg = _cfg;
			uId = _uId;
		}

		public void Save(long taskId, string text, string plainText) {
			long id = BLL.Cnn(cfg).NextId("TranscriptionHistoryWeb");
			BLL.Cnn(cfg).InsertSQL<TranscriptionHistoryWeb>(new { id, taskId, Date = DateTime.Now, UserId = uId, text = text.ToClob(), plainText = plainText.ToClob() });
		}
	}
}
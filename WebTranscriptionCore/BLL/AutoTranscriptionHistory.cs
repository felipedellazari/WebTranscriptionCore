using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebTranscriptionCore {
	public class AutoTranscriptionHistory {
		public int Status { get; set; }

		public readonly IConfiguration cfg;

		public AutoTranscriptionHistory(IConfiguration _cfg) => cfg = _cfg;
		public AutoTranscriptionHistory() { }

		public void Set(long? taskId, Guid transcriptionGuid, DateTime dateSend, DateTime? dateReceived, long? userId, TimeSpan duration, TimeSpan extraTimeTotal, int status, string licenceSerial, int providerId) {
			long id = BLL.Cnn(cfg).NextId(typeof(AutoTranscriptionHistory).Name);
			object args = new { id, taskId, transcriptionGuid = transcriptionGuid.ToString(), dateSend, userId, duration = duration.Ticks, extraTimeTotal = extraTimeTotal.Ticks, status, licenceSerial = (string)licenceSerial, providerId };
			BLL.Cnn(cfg).InsertSQL<AutoTranscriptionHistory>(args);
		}

		public AutoTranscriptionHistory LoadByTaskId(long? taskId) {
			string sql = @"SELECT th.Status
									FROM   AutoTranscriptionHistory th
									WHERE  th.TaskId = :taskId";
			AutoTranscriptionHistoryStatus result = BLL.Cnn(cfg).QueryFirst<AutoTranscriptionHistoryStatus>(sql, new { taskId });
			if (result == null) {
				return null;
			}
			var autoTranscriptionHistory = new AutoTranscriptionHistory() { 
				Status = result.Status
			};

			return autoTranscriptionHistory;
		}

		public void UpdateStatus(long? taskId, int status, DateTime? dateReceived) {

			object args = new { taskId, status, dateReceived };
			string sql = @"UPDATE AutoTranscriptionHistory 
									SET		Status = :status,
											DateReceived = :dateReceived
									WHERE    TaskId = :taskId";
			BLL.Cnn(cfg).Execute(sql, args);
		}

		private class AutoTranscriptionHistoryStatus {
				public int Status { get; set; }

		}
	}
}

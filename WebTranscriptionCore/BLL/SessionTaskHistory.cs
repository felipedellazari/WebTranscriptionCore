using Microsoft.Extensions.Configuration;
using System;

namespace WebTranscriptionCore {
	public class SessionTaskHistory {

		public readonly IConfiguration cfg;

		public SessionTaskHistory(IConfiguration _cfg) => cfg = _cfg;

		public void Set(SessionTask st, FlowStepStatus status, long? userId, long? userIdExecutioner, long? profileIdExecutioner) {
			long id = BLL.Cnn(cfg).NextId(typeof(SessionTaskHistory).Name);
			object args = new { id, taskid = st.Id, status = (int)status, logDate = DateTime.Now, userId, userIdExecutioner, profileIdExecutioner };
			BLL.Cnn(cfg).InsertSQL<SessionTaskHistory>(args);
		}
	}
}
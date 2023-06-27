using Microsoft.Extensions.Configuration;

namespace WebTranscriptionCore {
	public class SessionType {

		public IConfiguration cfg { get; set; }
		public long? Id { get; set; }
		public string Name { get; set; }

		public SessionType() { }

		public SessionType(long? id, IConfiguration cfg) {
			if (id != null) {
				SessionType sessionType = BLL.Cnn(cfg).QueryFirst<SessionType>(@"SELECT ID, NAME FROM SessionType WHERE Id = :id", new { id });
				Id = sessionType.Id;
				Name = sessionType.Name;
			}
		}
	}
}
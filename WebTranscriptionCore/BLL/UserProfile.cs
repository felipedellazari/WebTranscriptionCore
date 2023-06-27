using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace WebTranscriptionCore {
	public class UserProfile : BaseClass {

		public long Id { get; set; }

		public UserProfile() { }

		public UserProfile(IConfiguration cfg) : base(cfg) { }

		public void Load(UserProfile up) => Id = up.Id;

		public static IEnumerable<UserProfile> FindBySids(IConfiguration cfg, string[] groupSids) {
			string sql = "SELECT ID FROM UserProfile WHERE Active = 1 AND ForeignKey IN ('" + string.Join("','", groupSids) + "')";

			IEnumerable<UserProfile> lst = BLL.Cnn(cfg).Query<UserProfile>(sql, null);

			foreach (UserProfile item in lst) {
				UserProfile up = new UserProfile(cfg);
				up.Id = item.Id;
				yield return up;
			}

		}
	}
}
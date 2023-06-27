using Kenta.Utils;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebTranscriptionCore {
	public class UserK : BaseClass {

		public long? Id { get; set; }
		public bool Active { get; set; }
		public string Name { get; set; }
		public string Login { get; set; }
		public int PageCount { get; set; }
		public string Email { get; set; }
		public List<long> ProfileIds { get; set; }

		public string LicenseSerialNumber { get; set; }

		public dynamic args { get; set; }

		public static string SessionId { get; set; }

		public string UniqId { get; set; }

		public const String URL = "https://support.kenta.com.br";
		
		public UserK() { }

		public UserK(IConfiguration cfg) : base(cfg) { }

		public UserK(IConfiguration cfg, UserK userK) : base(cfg) { }

		public UserK(long id, IConfiguration cfg) : base(cfg) {
			UserK userK = BLL.Cnn(cfg).QueryFirst<UserK>(@"SELECT ID, ACTIVE, NAME, EMAIL, LOGIN FROM ""USER"" WHERE Id = :id", new { id });
			Load(userK);
		}

		public void Load(UserK userK) {
	
			Id = userK.Id;
			Active = BLL.ToBool(userK.Active);
			Name = userK.Name;
			Email = userK.Email;
			Login = userK.Login;
			LicenseSerialNumber = userK.LicenseSerialNumber;

			ProfileIds = new List<long>();
			IEnumerable<long> profs = BLL.Cnn(cfg).Query<long>("SELECT PROFILEID FROM User_UserProfile WHERE UserId = :Id", new { Id });
			foreach (long prof in profs)
				ProfileIds.Add(prof);
		}

		public void Save(string name, string email, string login, Password? password, string initials, string foreignKey, IEnumerable<UserProfile> profiles) {
			if (Id == null) {
				Id = BLL.Cnn(cfg).NextId("USER");
				BLL.Cnn(cfg).InsertSQL(@"""USER""", new { Id, Active = 1, name, email, login, password, initials, foreignKey });
			} else {
				BLL.Cnn(cfg).UpdateSQL(@"""USER""", new { Id, name, email, login, password, initials, foreignKey }, "id");
				BLL.Cnn(cfg).Execute("DELETE FROM User_UserProfile WHERE UserId = :Id", new { Id });
			}

			string sql = BLL.Cnn(cfg).InsertJunction("UserProfile", "User_UserProfile", Id.Value, profiles.Select(x => x.Id));
			BLL.Cnn(cfg).Execute(sql, null);

			Name = name;
		}

		public void SetProfiles(long[] profiles) {
			if (ProfileIds.ToArray().OrderBy(x => x).SequenceEqual(profiles.OrderBy(x => x))) return;
			BLL.Cnn(cfg).Execute("DELETE FROM User_UserProfile WHERE UserId = :Id", new { Id });
			string pids = string.Join(", ", profiles);
			if (pids == "") pids = "NULL";
			if (profiles.Length > 0) BLL.Cnn(cfg).Execute("INSERT INTO User_UserProfile SELECT " + Id + ", p.Id FROM UserProfile p WHERE p.Id IN (" + pids + ")", null);
			ProfileIds = profiles.ToList();
		}

		public string MakeInitials(string name) {
			string i = "";
			foreach (string s in name.Split(" "))
				i += s.Substring(0, 1);
			return i;
		}

		public static IEnumerable<IdName> ListAll(IConfiguration cfg, bool AllSelect = true) {
			string sql = "";
			if (AllSelect) {
				sql = @"SELECT ID, NAME FROM ""USER"" WHERE Active = 1 ORDER BY Name";
			}
			else
			{
				sql = @"SELECT ID, NAME FROM ""USER"" ORDER BY Name";
			}
			
			IEnumerable<IdName> lst = BLL.Cnn(cfg).Query<IdName>(sql, new { });
			return lst;
		}

		public static UserK GetByForeignKey(IConfiguration cfg, string fk) {
			string sql = @"SELECT ID, ACTIVE, NAME, EMAIL, LOGIN FROM ""USER"" WHERE ForeignKey = :fk";
			UserK userKFromDb = BLL.Cnn(cfg).QueryFirst<UserK>(sql, new { fk });

			if (userKFromDb == null) return null;
			UserK userK = new UserK(cfg);
			userK.Load(userKFromDb);
			return userK;
		}

		//novo com LICENSESERIALNUMBER

		//public static UserK GetByPassword(IConfiguration cfg, string user, string pwd_) {
		//	Password pwd = new Password(pwd_);
		//	string sql = @"SELECT ID, ACTIVE, NAME, EMAIL, LOGIN, LICENSESERIALNUMBER FROM ""USER"" WHERE Active = 1 AND Login = :login AND Password = :pwd";
		//	UserK userKFromDB = BLL.Cnn(cfg).QueryFirst<UserK>(sql, new { login = user, pwd = (byte[])pwd });

		//	if (userKFromDB == null) return null;
		//	UserK userK = new UserK(cfg);
		//	userK.Load(userKFromDB);
		//	return userK;
		//}


		//old sem coluna LICENSESERIALNUMBER
		
		public static UserK GetByPassword(IConfiguration cfg, string user, string pwd_)
		{
			Password pwd = new Password(pwd_);
			string sql = @"SELECT ID, ACTIVE, NAME, EMAIL, LOGIN FROM ""USER"" WHERE Active = 1 AND Login = :login AND Password = :pwd";


			UserK userKFromDB = BLL.Cnn(cfg).QueryFirst<UserK>(sql, new { login = user, pwd = (byte[])pwd });

			if (userKFromDB == null) return null;
			UserK userK = new UserK(cfg);
			userK.Load(userKFromDB);
			return userK;
		}
		

		public string RegistryLicenseAsync(string licenseSerialNumber)
		{
			string toReturn;
			string id = GetComposedId(this.Id.ToString());

			if (!ValidateLicenseAsync().Equals("Sucesso"))
			{

				using (HttpClient client2 = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
				{
					client2.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
					Task<HttpResponseMessage> resp2 = client2.PostAsync(new Uri(URL + "/licensing?" +
					"productName=" + "DRS Plenário Transcrição Web" +
					"&serialNumber=" + licenseSerialNumber +
					"&userName=" + this.Name +
					"&cpuId=" + id +
					"&machineName=" + this.Login +
					"&machineDescription=" + null +
					"&domainSid=" + null +
					"&systemReport=" + null), null);

					resp2.Wait();

					if (resp2.Result.StatusCode.Equals(HttpStatusCode.Accepted))
					{
						SaveUserLicenseSerialNumber(licenseSerialNumber);
						toReturn = "Sucesso";
					}
					else
					{
						toReturn = resp2.Result.StatusCode.ToString();
					}

					resp2.Dispose();
					return toReturn;
				}

			}else{

				toReturn = "Usuário já possui licença ativa!";
				return toReturn;
			}

		}

		public void SaveUserLicenseSerialNumber(string licenseSerialNumber)
		{
			BLL.Cnn(cfg).UpdateSQL(@"""USER""", new { Id, licenseSerialNumber }, "Id");
		}

		public string ValidateLicenseAsync()
		{
			string toReturn;
			string id;
			id = GetComposedId(this.Id.ToString());

			using (HttpClient client2 = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
			{
				client2.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
				Task<HttpResponseMessage> resp2 = client2.PostAsync(new Uri(URL + "/validation?" +
				"serial=" + this.LicenseSerialNumber +
				"&id=" + id), null);

				resp2.Wait();

				if (resp2.Result.StatusCode.Equals(HttpStatusCode.Accepted) || resp2.Result.StatusCode.Equals(HttpStatusCode.OK))
				{
					toReturn = "Sucesso";
				}
				else
				{
					toReturn = resp2.Result.StatusCode.ToString();
				}

				resp2.Dispose();
				return toReturn;
			}
		}

		public String GetComposedId(string id){
			string toReturn;
			toReturn = id.PadLeft(8, '0') + BLL.GetConfig("Customer_Id", cfg).PadLeft(8, '0');
			return toReturn;
		}

		public string VerifySimultaneousConnection()
		{
			string toReturn = SessionId;
			string sql = @"SELECT SessionId, UserId FROM ""CONNECTEDUSERS"" WHERE SessionId = :SessionId";
			UserK userKFromDb = BLL.Cnn(cfg).QueryFirst<UserK>(sql, new { SessionId });

			if (userKFromDb == null)
			{
				toReturn = "0";
			}

			return toReturn;
		}

		public void DeleteOldSessions(){
			string Id = GetComposedId(this.Id.ToString());
			BLL.Cnn(cfg).Execute(@"DELETE FROM ""CONNECTEDUSERS"" WHERE UserId = :Id", new { Id });
		}

		public string CreateSession()
		{
			string toReturn = "";
			string UserId = GetComposedId(this.Id.ToString());
			SessionId = UserId + DateTime.UtcNow + DateTime.UtcNow.Millisecond;
			BLL.Cnn(cfg).InsertSQL(@"""CONNECTEDUSERS""", new { SessionId, UserId });

			return toReturn;
		}

		public string GetCurrentSession(){
			return SessionId;
		}

		public bool IsConfidentialAllowed()
		{

			bool toReturn = false;

			if (Id == 0) return true;

			foreach (long pId in ProfileIds)
			{
				String sql = @"
				SELECT o.Id as OBJECTID, a.Id as ACTIONID
				FROM SystemAction a 
				INNER JOIN SystemObject o ON a.OwnerId = o.Id
				INNER JOIN Permission p ON p.ActionId = a.Id AND p.ObjectId = o.Id AND p.ProfileId = :pId";
				Object args = new { pId };
				foreach (var pl in BLL.Cnn(cfg).Query(sql, args))
				{
					//se igual a permissao de acessar sessoes sigilosas retorna true
					if (pl.OBJECTID == 503 && pl.ACTIONID == 1)
					{
						return true;
					}
				}
			}

			return toReturn;

		}

	}
}
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace WebTranscriptionCore {
	public class Session : BaseClass {

		private long? idParent;
		private long sessionTypeId;
		protected long flowTypeId;
		private Session parent;
		private SessionType sessionType;
		private FlowType flowType;
		IEnumerable<SessionTask> tasks;

		public long Id { get; set; }
		public string Description { get; set; }
		public DateTime ExpectedDate { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime FinishDate { get; set; }
		public string SessionTypeName { get; set; }
		public string PlaceName { get; set; }
		public string Number { get; set; }
		public SessionType SessionType {
			get {
				sessionType = sessionType ?? new SessionType(sessionTypeId, cfg);
				return sessionType;
			}
		}
		public Guid Guid { get; set; }

		public object GuidFromDB { get; set; }

		public long SessionId { get; set; }

		public string AudioFileName => GetGlobalPath() + "Audio.mp3";

		public Turn[] Turns { get; set; }

		public string TurnsFromDB { get; set; }
		public string FilesFromDB { get; set; }

		public TimeSpan Duration { get; set; }

		public long DurationFromDb { get; set; }
		public long TurnDurationFromDb { get; set; }
		public TimeSpan TurnDuration { get; set; }

		public IEnumerable<SessionTask> Tasks {
			get {
				if (tasks == null) tasks = SessionTask.GetbySession(cfg, claims, Id);
				return tasks;
			}
		}

		public Session Parent {
			get {
				parent = parent ?? new Session(idParent, cfg, claims);
				return parent;
			}
		}

		public SessionStatus Status { get; set; }
		public string StatusDesc => Status.GetDescription();

		public FlowType FlowType {
			get {
				flowType = flowType ?? new FlowType(cfg, flowTypeId);
				return flowType;
			}
		}

		public bool AutoTranscription { get; set; }

		public long? TypeId { get; set; }
		public long PlaceId { get; set; }

		public Session() { }

		public Session(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public Session(long? id, IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) {
			if ((id != null) && (id != 0)) {
				string sql = @"SELECT	s.ID, 
												s.DESCRIPTION, 
												s.EXPECTEDDATE, 
												s.STARTDATE, 
												s.FINISHDATE, 
												s.EXPECTEDDATE, 
												s.""NUMBER"", 
												s.TYPEID, 
												s.PLACEID, 
												s.GUID as GUIDFROMDB, 
												s.DURATION as DURATIONFROMDB, 
												s.TURNS as TURNSFROMDB, 
												s.FILES as FILESFROMDB, 
												s.SESSIONID, 
												s.STATUS, 
												st.FLOWTYPEID,
												st.NAME SESSIONTYPENAME ,
												p.NAME as PLACENAME,
												s.AUTOTRANSCRIPTION,
												sc.TURNDURATION as TURNDURATIONFROMDB
									FROM		""SESSION"" s 
												INNER JOIN SessionType st ON st.Id = s.TypeId
												LEFT JOIN Place p on s.PLACEID = p.ID
												LEFT JOIN Schedule sc on s.ScheduleId = sc.ID
									WHERE s.Id = :id";
				Session session = BLL.Cnn(cfg).QueryFirst<Session>(sql, new { id });
				Id = session.Id;
				Description = session.Description;
				ExpectedDate = session.ExpectedDate;
				StartDate = session.StartDate;
				FinishDate = session.FinishDate;
				Number = session.Number;
				sessionTypeId = session.TypeId.Value;
				if (session.GuidFromDB is string sGuid) Guid = Guid.Parse(sGuid);
				else if (session.GuidFromDB is Guid guid) Guid = guid;
				Duration = TimeSpan.FromTicks(session.DurationFromDb);
				TurnDuration = TimeSpan.FromTicks(session.TurnDurationFromDb);
				Turns = session.TurnsFromDB == null ? null : JsonConvert.DeserializeObject<Turn[]>(session.TurnsFromDB);
				idParent = session.SessionId;
				Status = (SessionStatus)session.Status;
				flowTypeId = session.flowTypeId;
				AutoTranscription = BLL.ToBool(session.AutoTranscription) && cfgs.AutoTransc;
				PlaceId = session.PlaceId;
				GuidFromDB = session.GuidFromDB;
				DurationFromDb = session.DurationFromDb;
				FilesFromDB = session.FilesFromDB;
				TypeId = session.TypeId;
				SessionTypeName = session.SessionTypeName;
				PlaceName = session.PlaceName;
			}
		}

		public int ListCount(DateTime? dateBegin, DateTime? dateEnd, string numberSession, string descriptionSesion, long? placeSessionId, string extraWhereConfidential) {
			int count = 0;
			List(true, ref count, null, null, dateBegin, dateEnd, numberSession, descriptionSesion, placeSessionId, extraWhereConfidential);
			return count;
		}

		public IEnumerable<Session> List(int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, string numberStatus, string descriptionSession, long? placeSessionId, string extraWhereConfidential) {
			int count = 0;
			return List(false, ref count, between1, between2, dateBegin, dateEnd, numberStatus, descriptionSession, placeSessionId, extraWhereConfidential);
		}

		private IEnumerable<Session> List(bool bCount, ref int iCount, int? between1, int? between2, DateTime? dateBegin, DateTime? dateEnd, string numberSession, string descriptionSession, long? placeSessionId, string extraWhereConfidential) {
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @" select  s.id, 
										s.guid, 
										s.""NUMBER"", 
										s.expecteddate, 
										s.status, 
										s.description, 
										s.duration, 
										s.startdate, 
										s.finishdate,
										p.name placeName,
										sp.name sessionTypeName";

			string sqlFrom = @" from ""SESSION"" s
									left join ""PLACE"" p on s.placeid = p.id
									left join ""SESSIONTYPE"" sp on s.typeid = sp.id ";

			string sqlWhere = " where s.Active = 1 AND s.SessionId IS NULL" + extraWhereConfidential;

			if (dateBegin != null && dateEnd != null) {
				sqlWhere += " AND s.ExpectedDate BETWEEN :dtBegin AND :dtEnd";
				args.Add("dtBegin", dateBegin.Value.Date);
				args.Add("dtEnd", dateEnd.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
			}
			if (numberSession != null && numberSession != "") {
				sqlWhere += @" AND s.""NUMBER"" LIKE '%' || :numberSession || '%'";
				args.Add("numberSession", numberSession);
			}
			if (descriptionSession != null && descriptionSession != "") {
				sqlWhere += @" AND s.description LIKE '%' || :descriptionSession || '%'";
				args.Add("descriptionSession", descriptionSession);
			}
			if (placeSessionId != null && placeSessionId != 0) {
				sqlWhere += " AND s.placeId = :placeId";
				args.Add("placeId", placeSessionId);
			}

			string sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere : "SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY s.ExpectedDate desc) RowNumCol " + sqlFrom + sqlWhere + ") tab WHERE RowNumCol BETWEEN :between1 AND :between2";
			args.Add("between1", between1);
			args.Add("between2", between2);

			List<Session> list = new List<Session>();

			if (bCount) {

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			} else {

				IEnumerable<SessionDAL> lst = BLL.Cnn(cfg).Query<SessionDAL>(sql, args);

				list = new List<Session>();
				foreach (SessionDAL item in lst) {
					Guid itemGuid = new Guid();
					if (item.Guid is string sGuid) itemGuid = Guid.Parse(sGuid);
					else if (item.Guid is Guid guid) itemGuid = guid;
					TimeSpan itemDuration = TimeSpan.FromTicks(item.Duration);

					list.Add(new Session() {
						Id = item.Id,
						Guid = itemGuid,
						Number = item.Number,
						Status = (SessionStatus)item.Status,
						Description = item.Description,
						Duration = itemDuration,
						PlaceName = item.PlaceName,
						SessionTypeName = item.SessionTypeName,
						ExpectedDate = item.ExpectedDate,
						StartDate = item.StartDate,
						FinishDate = item.FinishDate
					});
				}
			}
			return list;
		}

		public IEnumerable<Session> ListRamifications(long sessionId, string extraWhereConfidential) {
			Dictionary<string, object> args = new Dictionary<string, object>();

			string sqlSelect = @" select  s.id, 
										s.guid, 
										s.""NUMBER"", 
										s.expecteddate, 
										s.status, 
										s.description, 
										s.duration, 
										s.startdate, 
										s.finishdate,
										p.name placeName,
										sp.name sessionTypeName";

			string sqlFrom = @" from ""SESSION"" s
									left join ""PLACE"" p on s.placeid = p.id
									left join ""SESSIONTYPE"" sp on s.typeid = sp.id ";

			string sqlWhere = " where s.Active = 1" + extraWhereConfidential;

			sqlWhere += " AND s.sessionId = :sessionId";
			args.Add("sessionId", sessionId);

			string sql = "SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY s.ExpectedDate desc) RowNumCol " + sqlFrom + sqlWhere + ") tab";

			IEnumerable<SessionDAL> lst = BLL.Cnn(cfg).Query<SessionDAL>(sql, args);

			List<Session> list = new List<Session>();
			foreach (SessionDAL item in lst) {
				Guid itemGuid = new Guid();
				if (item.Guid is string sGuid) itemGuid = Guid.Parse(sGuid);
				else if (item.Guid is Guid guid) itemGuid = guid;
				TimeSpan itemDuration = TimeSpan.FromTicks(item.Duration);

				list.Add(new Session() {
					Id = item.Id,
					Guid = itemGuid,
					Number = item.Number,
					Status = (SessionStatus)item.Status,
					Description = item.Description,
					Duration = itemDuration,
					PlaceName = item.PlaceName,
					SessionTypeName = item.SessionTypeName,
					ExpectedDate = item.ExpectedDate,
					StartDate = item.StartDate,
					FinishDate = item.FinishDate
				});
			}
			return list;
		}

		public Session Get(int Id) {
			return new Session();
		}

		public string GetGlobalPath(bool physicalPath = false) {

			string path = physicalPath ? cfg.GetSection("StoragePath").Value : cfg.GetSection("StorageUrl").Value;
			string mask = BLL.GetConfig("Storage_Mask", cfg);
			if (!string.IsNullOrWhiteSpace(mask)) {
				string maskFormated = "";
				string formula = "";
				bool inFormula = false;
				foreach (char c in mask) {
					if (c == '»') {
						inFormula = false;
						if (formula.ToLower().Contains("expecteddate")) {
							if (formula.Contains(':')) {
								int idxTwoPoints = formula.IndexOf(":");
								string maskDate = formula.Substring(idxTwoPoints + 1, formula.Length - idxTwoPoints - 1);
								maskFormated += ExpectedDate.ToString(maskDate);
							} else {
								maskFormated += ExpectedDate.ToString();
							}
						} else if (formula.ToLower() == "number") {
							maskFormated += Number;
						} else if (formula.ToLower() == "description") {
							maskFormated += Description;
						} else if (formula.ToLower() == "type") {
							maskFormated += SessionType.Name;
						} else
							throw new Exception("Máscara inválida: " + mask);
					} else if (inFormula) {
						formula += c;
					} else if (c == '«') {
						formula = "";
						inFormula = true;
					} else {
						maskFormated += c;
					}
				}
				if (maskFormated.StartsWith(@"\"))
					maskFormated = maskFormated.Substring(1, maskFormated.Length - 1);
				if (physicalPath) {
					path += path.EndsWith(@"\") ? "" : @"\";
					path += maskFormated.Replace(@"/", @"\");
					path += path.EndsWith(@"\") ? "" : @"\";
					path += Guid.ToString("N") + @"\";
				} else {
					path += path.EndsWith(@"/") ? "" : @"/";
					path += maskFormated.Replace(@"\", @"/");
					path += path.EndsWith(@"/") ? "" : @"/";
					path += Guid.ToString("N") + @"/";
				}
			}
			return path;
		}
	}
}
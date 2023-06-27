using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public class SessionTask : BaseClass {

		protected long sessionId;
		protected long stepId;
		protected long typeId;

		private FlowType flowType;
		private Session session;

		public string Text { get; set; }
		public string PlainText { get; set; }

		public long? Id { get; set; }

		public string Description { get; set; }

		public FlowStepStatus Status { get; set; }

		public string StatusDesc => Status.GetDescription();

		public string AssignedTo { get; set; }
		public DateTime SessionExpectedDate { get; set; }

		public Session Session {
			get {
				session = session ?? new Session(sessionId, cfg, claims);
				return session;
			}
		}

		public string Arguments { get; set; }

		public long? AssignedUserId { get; set; }

		public long? AssignedProfileId { get; set; }

		public DateTime? StartDate { get; set; }

		public long Mark { get; set; }

		public long MarkMin { get; set; }

		public long MarkMax { get; set; }

		public bool FirstMark { get; set; }

		public int Count { get; set; }

		public string Extra { get; set; }

		public bool LastMark { get; set; }

		public FlowType FlowType {
			get {
				flowType = flowType ?? new FlowType(cfg, typeId);
				return flowType;
			}
		}

		public bool AllowTranscription{
			get {
				BaseTranscriptionTask st = SessionTask.GetById(cfg, claims, this.Id??0) as BaseTranscriptionTask;
				if (st.FlowStep.Is<BasePartialTranscriptionTask>() && st.AllowAutoTranscription) return true;
				else return false;
			}
		}

		public FlowStep FlowStep => FlowType.Steps.FirstOrDefault(x => x.Id == stepId);

		public long? StepIdPrior => FlowType.StepsJoin.FirstOrDefault(x => x.Next.Id == FlowStep.Id)?.Prior.Id;

		public string AutoTranscId { get; set; }

		public int? TranscribedIn { get; set; }

		public TranscribedInEnum TranscribedInEnum { get; set; }

		public TimeSpan AudioPosition { get; set; }

		public SessionTask() { }

		public SessionTask(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }

		public SessionTask(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims) => LoadTask(id);

		public SessionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims) => LoadTask(null, sessionId, stepId);

		public SessionTask(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims) => LoadTask(null, sessionId, stepId, mark);

		private void LoadTask(long? id = null, long? sessionId = null, long? stepId = null, long? mark = null) {
			string sql = @"SELECT		st.ID, 
												st.DESCRIPTION, 
												st.SESSIONID, 
												st.ARGUMENTS, 
												st.ASSIGNEDUSERID, 
												st.ASSIGNEDPROFILEID, 
												st.STATUS,
												st.STARTDATE,
												st.MARK,
												st.STEPID,
												st.EXTRA,
												fs.TYPEID,
												tdw.TRANSCRIBEDIN,
												(SELECT MIN(st2.Mark) FROM SessionTask st2 WHERE st2.SessionId = st.SessionId AND st2.StepId = st.StepId) MARKMIN,
												(SELECT MAX(st2.Mark) FROM SessionTask st2 WHERE st2.SessionId = st.SessionId AND st2.StepId = st.StepId) MARKMAX
									FROM SessionTask st 
												INNER JOIN FlowStep fs ON fs.Id = st.StepId
												LEFT JOIN TranscriptionDataWeb tdw ON tdw.TaskId = st.Id
									WHERE 1 = 1 ";
			if (id != null) sql += " AND st.Id = :id";
			if (sessionId != null) sql += " AND st.SessionId = :sessionId";
			if (stepId != null) sql += " AND st.StepId = :stepId";
			if (mark != null) sql += " AND st.Mark = :mark";
			SessionTask sessionTask = BLL.Cnn(cfg).QueryFirst<SessionTask>(sql, new { id, sessionId, stepId, mark });

			Load(sessionTask);
		}

		private void Load(SessionTask sessionTask) {
			Id = sessionTask.Id;
			Description = sessionTask.Description;
			this.sessionId = sessionTask.sessionId;
			Arguments = sessionTask.Arguments;
			AssignedUserId = sessionTask.AssignedUserId;
			AssignedProfileId = sessionTask.AssignedProfileId;
			Status = (FlowStepStatus)sessionTask.Status;
			StartDate = sessionTask.StartDate;
			this.stepId = sessionTask.stepId;
			typeId = sessionTask.typeId;
			Mark = sessionTask.Mark;
			FirstMark = sessionTask.MarkMin == Mark;
			LastMark = sessionTask.MarkMax == Mark;
			TranscribedInEnum = sessionTask.TranscribedIn == null ? TranscribedInEnum.NotTranscribed : ((TranscribedInEnum)sessionTask.TranscribedIn);
			Dictionary<string, object> extra = JsonConvert.DeserializeObject<Dictionary<string, object>>(sessionTask.Extra ?? "") ?? new Dictionary<string, object>();
			AudioPosition = TimeSpan.FromTicks(Convert.ToInt64(extra.FirstOrDefault(x => x.Key == "Audio").Value ?? 0));
			AutoTranscId = extra.FirstOrDefault(x => x.Key == "TranscriptionId").Value == null  ? null : extra.First(x => x.Key == "TranscriptionId").Value.ToString();
		}

		public int ListCount(DateTime? dateSession, string timeSession, byte taskStatus, long? assignedUserId, string extraWhere) {
			int count = 0;
			List(true, ref count, null, null, dateSession, timeSession, taskStatus, assignedUserId, extraWhere);
			return count;
		}
		public IEnumerable<SessionTask> List(int? between1, int? between2, DateTime? dateSession, string timeSession, byte taskStatus, long? assignedUserId, string extraWhere) {
			int count = 0;
			return List(false, ref count, between1, between2, dateSession, timeSession, taskStatus, assignedUserId, extraWhere);
		}

		private IEnumerable<SessionTask> List(bool bCount, ref int iCount, int? between1, int? between2, DateTime? dateSession, string timeSession, byte taskStatus, long? assignedUserId, string extraWhere) {
			Dictionary<string, object> args = new Dictionary<string, object>();
			string sqlSelect = @" SELECT	st.ID, 
													st.DESCRIPTION,
													st.STATUS,
													COALESCE(u.Name, up.Name) AS ASSIGNEDTO,
													st.EXTRA,
													s.EXPECTEDDATE AS SESSIONEXPECTEDDATE";

			string sqlFrom = @" FROM		SessionTask st 
														INNER JOIN	""SESSION"" s	ON s.Id = st.SessionId
														INNER JOIN	FlowStep fs		ON fs.Id = st.StepId
														LEFT JOIN	""USER"" u		ON u.Id = st.AssignedUserId
														LEFT JOIN	UserProfile up	ON up.Id = st.AssignedProfileId ";
			string sqlWhere = @" WHERE		s.Active = 1 AND
													fs.WebTranscription = 1" + extraWhere;
			if (dateSession != null) {
				if (timeSession.Cleanup() == null) {
					sqlWhere += " AND s.ExpectedDate BETWEEN :dtBegin AND :dtEnd";
					args.Add("dtBegin", dateSession.Value.Date);
					args.Add("dtEnd", dateSession.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
				} else {
					sqlWhere += " AND s.ExpectedDate = :dateSession";
					args.Add("dateSession", dateSession.Value.Date.AddHours(Convert.ToDouble(timeSession.Split(':')[0])).AddMinutes(Convert.ToDouble(timeSession.Split(':')[1])).AddSeconds(0));
				}
			}
			if (taskStatus >= 2) {
				List<FlowStepStatus> lSta = new List<FlowStepStatus>();
				if (taskStatus == 2)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Active, FlowStepStatus.Revoked, FlowStepStatus.Paused, FlowStepStatus.Reopened, FlowStepStatus.Correct, FlowStepStatus.Ratified, FlowStepStatus.Executing });
				else if (taskStatus == 3)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Waiting, FlowStepStatus.Active, FlowStepStatus.Executing, FlowStepStatus.Paused, FlowStepStatus.Revoked });
				else if (taskStatus == 4)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Waiting });
				else if (taskStatus == 5)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Active, FlowStepStatus.Executing, FlowStepStatus.Paused, FlowStepStatus.Revoked });
				else if (taskStatus == 6)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Revoked });
				else if (taskStatus == 7)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Done });
				else if (taskStatus == 8)
					lSta.AddRange(new FlowStepStatus[] { FlowStepStatus.Canceled });
				sqlWhere += @" AND st.Status IN (" + string.Join(",", lSta.Select(x => ((int)x).ToString())) + ")";
			}
			if (assignedUserId == uId) {
				sqlWhere += " AND (st.AssignedUserId = :CurrentUserId1 OR (st.AssignedProfileId IN (SELECT ProfileId FROM User_UserProfile WHERE UserId = :CurrentUserId2)))";
				args.Add("CurrentUserId1", uId);
				args.Add("CurrentUserId2", uId);
			} else if (assignedUserId != null) {
				sqlWhere += " AND st.AssignedUserId = :assignedUserId";
				args.Add("assignedUserId", assignedUserId);
			}
			string sql = bCount ? "SELECT COUNT(*) COUNT " + sqlFrom + sqlWhere : "SELECT * FROM (" + sqlSelect + ", ROW_NUMBER() OVER (ORDER BY s.ExpectedDate, st.StepId, st.Mark) RowNumCol " + sqlFrom + sqlWhere + ") tab WHERE RowNumCol BETWEEN :between1 AND :between2";
			args.Add("between1", between1);
			args.Add("between2", between2);

			List<SessionTask> list = new List<SessionTask>();

			if (bCount) {

				IEnumerable<int> lst = BLL.Cnn(cfg).Query<int>(sql, args);

				iCount = Convert.ToInt32(lst.FirstOrDefault());
				return null;
			} else {

				IEnumerable<SessionTask> lst = BLL.Cnn(cfg).Query<SessionTask>(sql, args);

				list = new List<SessionTask>();

				foreach (SessionTask item in lst) {
					Dictionary<string, object> extra = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.Extra ?? "") ?? new Dictionary<string, object>();
					list.Add(new SessionTask(cfg, claims) {
						Id = item.Id,
						Description = item.Description,
						Status = (FlowStepStatus)item.Status,
						AssignedTo = item.AssignedTo,
						AutoTranscId = extra.FirstOrDefault(x => x.Key == "TranscriptionId").Value == null ? null : extra.First(x => x.Key == "TranscriptionId").Value.ToString(),
						SessionExpectedDate = item.SessionExpectedDate
					});
				}

			}
			return list;
		}

		public virtual bool BeforeExecute() {
			if (!Id.HasValue) throw new ValidationException("Id da tarefa não localizado.");
			long[] profileIds = JsonConvert.DeserializeObject<long[]>(claims.First(x => x.Type == "UserProfileIds").Value);
			if (AssignedUserId != uId && !profileIds.Contains(AssignedProfileId ?? -1))
				throw new ValidationException("Tarefa delegada para outro usuario ou perfil.");
			if (Status == FlowStepStatus.Done || Status == FlowStepStatus.Canceled || Status == FlowStepStatus.Waiting)
				throw new ValidationException("Status da tarefa inválido.");
			return true;
		}

		public virtual void Execute() {
			if (!BeforeExecute()) return;
			if (Status != FlowStepStatus.Executing) {
				long uId = Convert.ToInt64(claims.First(x => x.Type == "userId").Value);
				FlowStepStatus status = FlowStepStatus.Executing;
				long? assignedUserId = uId;
				DateTime startDate = DateTime.Now;
				object args = new { status, assignedUserId, startDate, Id };
				string sql = @"UPDATE	SessionTask 
									SET		Status = :status,
												AssignedUserId = :assignedUserId, 
												AssignedProfileId = NULL,
												StartDate = :startDate 
									WHERE    Id = :Id AND
												(AssignedUserId IS NULL OR AssignedUserId = :assignedUserId)";
				int rowsA = BLL.Cnn(cfg).Execute(sql, args);
				if (rowsA == 0) throw new ValidationException("Tarefa está sendo executada por outro usuário");
				Status = status;
				StartDate = startDate;
				AssignedUserId = assignedUserId;
				AssignedProfileId = null;
				new SessionTaskHistory(cfg).Set(this, status, uId, AssignedUserId, AssignedProfileId);
			}
		}

		public void Pause(decimal progress, TimeSpan audioPosition) {
			if (AssignedUserId != uId)
				throw new ValidationException("Tarefa delegada para outro usuario.");
			if (Status == FlowStepStatus.Done || Status == FlowStepStatus.Canceled || Status == FlowStepStatus.Waiting)
				throw new ValidationException("Status da tarefa inválido.");
			if (Status == FlowStepStatus.Executing) {
				Dictionary<string, object> extra = new Dictionary<string, object>();
				extra.Add("Audio", audioPosition.Ticks);
				if (AutoTranscId != null) extra.Add("TranscriptionId", AutoTranscId);
				BLL.Cnn(cfg).UpdateSQL<SessionTask>(new {
					Duration = (StartDate.HasValue ? new TimeSpan(DateTime.Now.Ticks - StartDate.Value.Ticks) : new TimeSpan()).Ticks,
					Status = (int)FlowStepStatus.Paused,
					progress,
					Extra = JsonConvert.SerializeObject(extra),
					Id
				}, "Id");
				new SessionTaskHistory(cfg).Set(this, FlowStepStatus.Paused, uId, AssignedUserId, AssignedProfileId);
			}
		}

		public void UpdateAutoTranscId(string autoTranscId) {
			Dictionary<string, object> extra = new Dictionary<string, object>();
			extra.Add("Audio", AudioPosition.Ticks);
			if (autoTranscId != null) extra.Add("TranscriptionId", autoTranscId);
			BLL.Cnn(cfg).UpdateSQL<SessionTask>(new {
				Extra = JsonConvert.SerializeObject(extra),
				Id
			}, "Id");
		}


		public void Accept() {
			if (AssignedUserId != uId)
				throw new ValidationException("Tarefa delegada para outro usuario.");
			if (Status == FlowStepStatus.Done || Status == FlowStepStatus.Canceled || Status == FlowStepStatus.Waiting)
				throw new ValidationException("Status da tarefa inválido.");
			Status = FlowStepStatus.Done;
			BLL.Cnn(cfg).UpdateSQL<SessionTask>(new { Status = (int)FlowStepStatus.Done, Progress = 1, AcceptDate = DateTime.Now, Id }, "Id");
			new SessionTaskHistory(cfg).Set(this, Status, uId, AssignedUserId, AssignedProfileId);
			ActivateNext();
		}

		public void Activate() {
			FlowStepStatus status = Status.In(FlowStepStatus.Waiting, FlowStepStatus.Canceled) ? FlowStepStatus.Active : FlowStepStatus.Ratified;
			BLL.Cnn(cfg).UpdateSQL<SessionTask>(new { Status = (int)status, ActivationDate = DateTime.Now, Id }, "Id");
			new SessionTaskHistory(cfg).Set(this, Status, uId, AssignedUserId, AssignedProfileId);
		}

		private void ActivateNext() {
			FlowStep nextStep = FlowType.StepsJoin.FirstOrDefault(x => x.Prior.Id == FlowStep.Id)?.Next;
			if (nextStep == null) {
				string sql = @"SELECT 1 FROM SessionTask t WHERE t.SessionId = :sessionId AND t.Active = 1 AND t.Status NOT IN (" + (int)FlowStepStatus.Done + "," + (int)FlowStepStatus.Canceled + ") ";
				if (BLL.Cnn(cfg).ExecScalar<byte?>(sql, new { sessionId }) == null) {
					BLL.Cnn(cfg).UpdateSQL(@"""SESSION""", new { Status = (int)SessionStatus.Complete, Id = sessionId }, "Id");
				}
			} else {
				if (nextStep.Multiple) {
					SessionTask stNext = new SessionTask(cfg, claims, sessionId, nextStep.Id, Mark);
					stNext.Activate();
				} else {
					string sql = "SELECT 1 FROM SessionTask WHERE SessionId = :sessionId AND Active = 1 AND StepId = :stepId AND Status <> :statusDone";
					Dictionary<string, object> args = new Dictionary<string, object>();
					args.Add("sessionId", sessionId);
					args.Add("stepId", FlowStep.Id);
					args.Add("statusDone", (int)FlowStepStatus.Done);
					if (BLL.Cnn(cfg).ExecScalar<byte?>(sql, args) == null) {
						SessionTask stNext = new SessionTask(cfg, claims, sessionId, nextStep.Id);
						stNext.Activate();
					}
				}
			}
		}

		public void ChangeStatus(FlowStepStatus status) {
			BLL.Cnn(cfg).UpdateSQL<SessionTask>(new { status = (int)status, Id }, "Id");
			new SessionTaskHistory(cfg).Set(this, status, uId, AssignedUserId, AssignedProfileId);
		}

		public static IEnumerable<SessionTask> GetbySession(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId) {
			string sql = @"SELECT		st.ID, 
												st.DESCRIPTION, 
												st.SESSIONID, 
												st.ARGUMENTS, 
												st.ASSIGNEDUSERID, 
												st.ASSIGNEDPROFILEID, 
												st.STATUS,
												st.STARTDATE,
												st.MARK,
												st.STEPID,
												st.EXTRA,
												fs.TYPEID,
												tdw.TRANSCRIBEDIN,
												(SELECT MIN(st2.Mark) FROM SessionTask st2 WHERE st2.SessionId = st.SessionId AND st2.StepId = st.StepId) MARKMIN,
												(SELECT MAX(st2.Mark) FROM SessionTask st2 WHERE st2.SessionId = st.SessionId AND st2.StepId = st.StepId) MARKMAX
									FROM SessionTask st 
												INNER JOIN FlowStep fs ON fs.Id = st.StepId
												LEFT JOIN TranscriptionDataWeb tdw ON tdw.TaskId = st.Id
									WHERE    fs.WebTranscription = 1 AND
												st.SessionId = :sessionId";
			IEnumerable<SessionTask> lst = BLL.Cnn(cfg).Query<SessionTask>(sql, new { sessionId });
			FlowType ft = FlowType.GetById(cfg, lst.FirstOrDefault().typeId);
			List<SessionTask> tasks = new List<SessionTask>();
			foreach (SessionTask item in lst) {
				SessionTask st = GetByClassName(ft.Steps.FirstOrDefault(x => x.Id == item.stepId), new object[] { cfg, claims });
				st.Load(item);
				tasks.Add(st);
			}
			return tasks;
		}

		public static SessionTask GetById(IConfiguration cfg, IEnumerable<Claim> claims, long id) {
			SessionTask st = new SessionTask(cfg, claims, id);
			FlowStep step = st.FlowStep;
			return GetByClassName(step, new object[] { cfg, claims, id });
		}

		private static SessionTask GetByClassName(FlowStep step, object[] args) {
			if (step.IsCustomization) {
				//Para customizações:
				//	- Criar um projeto do tipo class library (.net core).
				//	- Adicionar referencia para o projeto principal.
				//	- Configurar para ele compilar na mesma pasta que compila o projeto principal.
				//	- Criar a classe customizada (ex.: TranscriptonTask2), e estender ela da classe conforme no plenario.
				// - Criar a classe customizada com o namespace Kenta.DRSPlenario

				Assembly a = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), step.AssembyName + ".dll"));
				Type t = a.GetType(step.ClassNameFull);
				return Activator.CreateInstance(t, args) as SessionTask;
			} else {
				if (step.InstanceType == null) throw new ValidationException($"Classe { step.ClassName } não implementada.");
				SessionTask stTask = (SessionTask)Activator.CreateInstance(step.InstanceType, args);
				return stTask;
			}
		}
	}
}
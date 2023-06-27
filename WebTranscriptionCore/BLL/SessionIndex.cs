using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace WebTranscriptionCore {
	public class SessionIndex : BaseClass {

		public long Id { get; set; }
		public int SessionId { get; set; }
		public int? AttendantId { get; set; }
		public string OradorName { get; set; }
		public long Offset { get; set; }
		public long? Duration { get; set; }
		public string Subject { get; set; }
		public string Stage { get; set; }
		public string SpeechType { get; set; }
		public DateTime? RecordDate { get; set; }
		public bool Confidential { get; set; }
		public long UserId { get; set; }
		public int? PresidenteId { get; set; }
		public string PresidenteName { get; set; }
		public int? RelatorId { get; set; }
		public string RelatorName { get; set; }
		public int? ClasseId { get; set; }
		public string ClasseName { get; set; }
		public int? RecursoId { get; set; }
		public string RecursoName { get; set; }
		public int? MateriaId { get; set; }
		public string MateriaName { get; set; }
		public string ProcessNumber { get; set; }
		public string Observation { get; set; }

		public static void UpdateIndexDuration(IConfiguration cfg, IEnumerable<Claim> claims, SessionIndex idx) {
			long duration = 0;
			if (idx == null) return;
			SessionIndex idxPrior = null;
			SessionIndex idxNext = null;
			IEnumerable<SessionIndex> equalsOffsetPrior = null;
			Session session = new Session(idx.SessionId, cfg, claims);
			long sessionDuration = session.Duration.Ticks;
			var sessionIndexes = SessionIndex.List(cfg, claims, idx.SessionId);

			//Força a atualização das durações indexs...
			idxPrior = sessionIndexes.LastOrDefault(x => x.Offset < idx.Offset);
			idxNext = sessionIndexes.FirstOrDefault(x => x.Offset > idx.Offset);
			//Atualiza o anterior	
			if (idxPrior != null) {
				equalsOffsetPrior = sessionIndexes.Where(i => i.Offset == idx.Offset).ToList();
				if (equalsOffsetPrior.Count() == 1) {
					//Correção da duração dos index no caso de houver vários indexs com o mesmo offset
					//Quando insere o próximo atualiza a duração dos anteriores
					foreach (SessionIndex item in sessionIndexes.Where(i => i.Offset == idxPrior.Offset)) {
						duration = idx.Offset - idxPrior.Offset;
						UpdateDuration(cfg, item.Id, duration);
					}
				}
				duration = idx.Offset - idxPrior.Offset;
				UpdateDuration(cfg, idxPrior.Id, duration);
			}
			//Atualiza o próximo
			if (idxNext != null) {
				duration = idxNext.Offset - idx.Offset;
				UpdateDuration(cfg, idx.Id, duration);
			}
			//Atualiza o ultimo index gravação com duracao
			if (idxNext == null && idxPrior != null && duration != new TimeSpan(0).Ticks)
				if (equalsOffsetPrior == null || equalsOffsetPrior.Count() == 1) {
					duration = sessionDuration - idx.Offset;
					UpdateDuration(cfg, idx.Id, duration);
				} else {
					//Correção da duração dos index no caso de houver vários indexs com o mesmo offset
					//Quando isere o próximo atualiza a duração dos anteriores
					foreach (SessionIndex g in equalsOffsetPrior) {
						duration = sessionDuration - idx.Offset;
						UpdateDuration(cfg, g.Id, duration);

					}
				}

			//Atualiza o unico index da gravação com duração
			if (idxNext == null && idxPrior == null && sessionDuration != new TimeSpan(0).Ticks) {
				duration = sessionDuration - idx.Offset;
				UpdateDuration(cfg, idx.Id, duration);
			}
		}

		public static void UpdateDuration(IConfiguration cfg, long Id, long duration) {
			if (duration <= 0) throw new Exception("Ocorreu um erro ao calcular a duração do índice.");
			BLL.Cnn(cfg).UpdateSQL<SessionIndex>(new { Id, duration }, "Id");
		}

		public static IEnumerable<SessionIndex> List(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId) {

			Dictionary<string, object> args = new Dictionary<string, object>();

			string sql = @"SELECT	si.ID, 
									si.SESSIONID, 
									si.ATTENDANTID, 
									si.OFFSET, 
									si.DURATION, 
									si.SUBJECT, 
									si.RECORDDATE,
									si.PROCESSNUMBER,
									si.PRESIDENTEID,
									si.RELATORID,
									si.CLASSEID,
									si.RECURSOID,
									si.MATERIAID,
									si.OBSERVATION,
                                    (select a.name from sessionattendant sa LEFT JOIN attendant a ON sa.Attendantid = a.id where sa.id = si.attendantid) as ORADORNAME,
                                    (select a.name from sessionattendant sa LEFT JOIN attendant a ON sa.Attendantid = a.id where sa.id = si.presidenteid) as PRESIDENTENAME,
                                    (select a.name from sessionattendant sa LEFT JOIN attendant a ON sa.Attendantid = a.id where sa.id = si.relatorid) as RELATORNAME,
                                    (select name from classe where id = si.classeid) as CLASSENAME,
                                    (select name from recurso where id = si.recursoid) as RECURSONAME,
                                    (select name from materia where id = si.materiaid) as MATERIANAME
							FROM SessionIndex si
							WHERE    si.SessionId = :sessionId
							ORDER BY OFFSET ASC";

			List<SessionIndex> list = new List<SessionIndex>();
			IEnumerable<SessionIndex> lst = BLL.Cnn(cfg).Query<SessionIndex>(sql, new { sessionId });

			foreach (SessionIndex item in lst) {

				list.Add(new SessionIndex() {
					Id = item.Id,
					SessionId = item.SessionId,
					AttendantId = item.AttendantId,
					Offset = item.Offset,
					Duration = item.Duration,
					Subject = item.Subject,
					RecordDate = item.RecordDate,
					ProcessNumber = item.ProcessNumber,
					PresidenteId = item.PresidenteId,
					RelatorId = item.RelatorId,
					ClasseId = item.ClasseId,
					MateriaId = item.MateriaId,
					Observation = item.Observation,
					PresidenteName = item.PresidenteName,
					RelatorName = item.RelatorName,
					OradorName = item.OradorName,
					ClasseName = item.ClasseName,
					RecursoName = item.RecursoName,
					MateriaName = item.MateriaName
				});
			}

			return list;
		}

		public static long Insert(IConfiguration cfg, IEnumerable<Claim> claims, SessionIndex sessionIndex) {

			string sqlId = "SELECT SQ_SESSIONINDEX.NEXTVAL FROM DUAL";
			var indexId = BLL.Cnn(cfg).Query<long>(sqlId, new { }).SingleOrDefault();

			string sql = @"INSERT INTO SessionIndex (ID, SESSIONID, OFFSET, ATTENDANTID, DURATION, RECORDDATE, SUBJECT, STAGE, SPEECHTYPE, USERID, CONFIDENTIAL, PRESIDENTEID, RELATORID, CLASSEID, RECURSOID, MATERIAID, PROCESSNUMBER, OBSERVATION) 
				VALUES (:IndexId, :SessionId, :Offset, :AttendantId, :Duration, :RecordDate, :Subject, :Stage, :SpeechType, :UserId, :Confidential, :PresidenteId, :RelatorId, :ClasseId, :RecursoId, :MateriaId, :ProcessNumber, :Observation)";

			sessionIndex.Duration = 10;
			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("IndexId", indexId);
			args.Add("SessionId", sessionIndex.SessionId);
			args.Add("Offset", sessionIndex.Offset);
			args.Add("AttendantId", sessionIndex.AttendantId);
			args.Add("Duration", sessionIndex.Duration);
			args.Add("RecordDate", sessionIndex.RecordDate);
			args.Add("Subject", sessionIndex.Subject);
			args.Add("Stage", sessionIndex.Stage);
			args.Add("SpeechType", sessionIndex.SpeechType);
			args.Add("UserId", sessionIndex.UserId);
			args.Add("Confidential", sessionIndex.UserId);
			args.Add("PresidenteId", sessionIndex.PresidenteId);
			args.Add("RelatorId", sessionIndex.RelatorId);
			args.Add("ClasseId", sessionIndex.ClasseId);
			args.Add("RecursoId", sessionIndex.RecursoId);
			args.Add("MateriaId", sessionIndex.MateriaId);
			args.Add("ProcessNumber", sessionIndex.ProcessNumber);
			args.Add("Observation", sessionIndex.Observation);

			BLL.Cnn(cfg).Execute(sql, args);
			UpdateIndexDuration(cfg, claims, sessionIndex);

			return indexId;
		}
	}
}
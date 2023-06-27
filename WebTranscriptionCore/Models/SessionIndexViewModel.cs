using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class SessionIndexViewModel {
		public long Id { get; set; }
		public int SessionId { get; set; }
		public int? AttendantId { get; set; }
		public long Offset { get; set; }
		public long OffsetEdited { get; set; }
		public int FileIndex { get; set; }
		public long? Duration { get; set; }
		public string Subject { get; set; }
		public int? PresidenteId { get; set; }
		public int? RelatorId { get; set; }
		public int? ClasseId { get; set; }
		public int? RecursoId { get; set; }
		public int? MateriaId { get; set; }
		public string ProcessNumber { get; set; }
		public string Observation { get; set; }
		public Attendant Orador { get; set; }
		public Attendant Presidente { get; set; }
		public Attendant Relator { get; set; }
		public Classe Classe { get; set; }
		public Recurso Recurso { get; set; }
		public Materia Materia { get; set; }


		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
		public DateTime? RecordDate { get; set; }

		public virtual SessionAttendantViewModel Attendant { get; set; }
		public virtual PlaySessionViewModel Session { get; set; }

		public virtual KeyValuePair<Int32, TimeSpan>? OffsetLocal {
			get {
				//Quando o indice esta fora dos arquivos já inseridos
				if (this.Offset >= this.Session.Duration) return null;
				//Calculando offset
				var f = JsonConvert.DeserializeObject<List<SessionIndexFile>>(this.Session.Files);
				Int32 max = this.Session.Indexes.IndexOf(this);
				if (max == -1) return null;
				Int32 v = 0;
				TimeSpan d = TimeSpan.Zero;
				var teste = f[0].Duration;
				for (Int32 i = 0; i < f.Count; i++) {
					if ((TimeSpan.FromTicks(this.Offset) - d) < TimeSpan.FromTicks(f[v].Duration)) continue;
					d += TimeSpan.FromTicks(f[v].Duration);
					v++;
				}//for
				return new KeyValuePair<Int32, TimeSpan>(v, TimeSpan.FromTicks(this.Offset) - d);
			}
		}

		private class SessionIndexFile {
			public string FileName { get; set; }
			public long Size { get; set; }
			public string Hash { get; set; }
			public long Duration { get; set; }
		}

	}
}
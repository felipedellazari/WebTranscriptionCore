using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTranscriptionCore {
	public class PlaySessionViewModel {
		public long Id { get; set; }
		public long PlaceId { get; set; }
		public long TypeId { get; set; }
		public string Guid { get; set; }
		public string ForeignKey { get; set; }
		public string Number { get; set; }
		public string Description { get; set; }
		public int Active { get; set; }
		public long? Duration { get; set; }
		public string Files { get; set; }
		public long FlowTypeId { get; set; }
		public Turn[] Turns { get; set; }
		public bool AutoTranscription { get; set; }
		public List<long> DurationsToAdd { get; set; }
		public string StoragePath { get; set; }
		public long StartIndexId { get; set; }
		public SessionStatus Status { get; set; }



		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
		public DateTime ExpectedDate { get; set; }

		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
		public DateTime? StartDate { get; set; }

		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
		public DateTime? FinishDate { get; set; }

		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
		public DateTime? PublishDate { get; set; }


		public virtual ICollection<SessionAttendantViewModel> Attendants { get; set; }
		public virtual List<SessionIndexViewModel> Indexes { get; set; }
		public virtual SessionTypeViewModel SessionType { get; set; }
		public virtual PlaceViewModel Place { get; set; }

		// Propriedades do índice
		public int IndexSessionId { get; set; }
		public string IndexOffset { get; set; }
		public int IndexFileIndex { get; set; }
		public long? IndexDuration { get; set; }
		[Display(Name = "Assunto")] 
		public string IndexSubject { get; set; }
		[Display(Name = "Fase")]
		public string IndexStage { get; set; }
		[Display(Name = "Tipo Pronunc.")]
		public string IndexSpeechType { get; set; }
		[Display(Name = "Sigiloso")]
		public bool IndexConfidential { get; set; }
		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
		public DateTime? IndexRecordDate { get; set; }
		[Display(Name = "Orador")]
		public string IndexAttendantId { get; set; }
		public IEnumerable<IdName> IndexAttendantList { get; set; }
		[Display(Name = "Presidente")]
		public string IndexPresidentetId { get; set; }
		public IEnumerable<IdName> IndexPresidenteList { get; set; }
		[Display(Name = "Relator")]
		public string IndexRelatorId { get; set; }
		public IEnumerable<IdName> IndexRelatorList { get; set; }
		[Display(Name = "Classe")]
		public string IndexClasseId { get; set; }
		public IEnumerable<IdName> IndexClasseList { get; set; }
		[Display(Name = "Recurso")]
		public string IndexRecursoId { get; set; }
		public IEnumerable<IdName> IndexRecursoList { get; set; }
		[Display(Name = "Materia")]
		public string IndexMateriaId { get; set; }
		public IEnumerable<IdName> IndexMateriaList { get; set; }
		[Display(Name = "Nro Processo")]
		public string ProcessNumber { get; set; }
		[Display(Name = "Observação")]
		public string Observation { get; set; }


	}
}
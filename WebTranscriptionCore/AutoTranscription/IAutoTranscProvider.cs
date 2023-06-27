using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WebTranscriptionCore {
	public interface IAutoTranscProvider {
		CreateJobResult CreateTranscriptionJob(long taskId, string mp3File, TimeSpan totalAudioDuration);
		IJobDetails GetTranscriptionJobDetails(string transcriptionId);
		bool IsEnabled { get; }
		AEngineStatus IsOnline { get; }
	}
	public enum CreateJobStatus {
		Success,
		Error,
		Disabled,
		Offline
	}
	public interface IJobDetails {
		JobStatus Status { get; }
		DateTime? DateEnd { get; set; }
		string Transcription { get; }
		AEngineStatus IsOnline { get; }
		string Message { get; }
	}
	[Flags]
	public enum JobStatus {
		None = 0,
		Queued = 1,
		Transcribing = 2,
		Transcribed = 4,
		Aborting = 8,
		Aborted = 16,
		Failed = 32,
		Unkown = 64,
		NotFound = 128
	}
	public abstract class AEngineStatus {
		public virtual EngineStatusEnum Status { get; set; } = EngineStatusEnum.Unkown;
		public virtual int QueueSize { get; set; }
		public string MessageStatus { get; set; }
		public bool IsOnline => (Status & EngineStatusEnum.Online) != 0;
		public static implicit operator bool(AEngineStatus v) {
			return v.IsOnline;
		}
	}
	[Flags]
	public enum EngineStatusEnum {
		None = 0,
		Idle = 1,
		Working = 2,
		Restarting = 4,
		Reconnecting = 8,
		Unkown = 16,
		Online = Idle | Working
	}
	public class CreateJobResult {
		[JsonIgnore()]
		public CreateJobStatus Status { get; set; }
		[JsonProperty("reference")]
		public string Reference { get; set; }
		public string Message { get; set; }
		public CreateJobResult() { }
		public CreateJobResult(CreateJobStatus status) => Status = status;
		public CreateJobResult(CreateJobStatus status, string message) {
			Status = status;
			Message = message;
		}
	}
	public class JobDetails : IJobDetails {
		public JobStatus Status { get; set; }
		public DateTime? DateEnd { get; set; }
		public string Transcription { get; set; }
		public AEngineStatus IsOnline { get; set; }
		public string Message { get; set; }
	}
	public class Video {
	}
	public class Metadata {
		public DateTime modifiedTime { get; set; }
		public string name { get; set; }
		public int priority { get; set; }
		public int sla { get; set; }
		public string status { get; set; }
		public string sync { get; set; }
		public DateTime transcriptionDate { get; set; }
		public Video video { get; set; }
	}
	public class TextFormat {
		public string fontColor { get; set; }
		public string fontFamily { get; set; }
		public int fontSize { get; set; }
		public int format { get; set; }
		public int length { get; set; }
		public int paragraph { get; set; }
		public int start { get; set; }
		public int typographicalList { get; set; }
	}
	public class DisplayFormat {
		public List<TextFormat> textFormats { get; set; }
	}
	public class Word {
		public double begin { get; set; }
		public double end { get; set; }
		public int pos { get; set; }
		public double score { get; set; }
		public string word { get; set; }
	}
	public class Part {
		public int align { get; set; }
		public double cpuTime { get; set; }
		public bool diarization { get; set; }
		public int displayPosition { get; set; }
		public string displaytext { get; set; }
		public bool hasChanges { get; set; }
		public bool needResync { get; set; }
		public int pos { get; set; }
		public string speaker { get; set; }
		public string type { get; set; }
		public string _id { get; set; }
		public double? begin { get; set; }
		public int? channel { get; set; }
		public DisplayFormat displayFormat { get; set; }
		public double? end { get; set; }
		public List<Word> words { get; set; }
	}
	public class QualityCounts {
		public int good { get; set; }
		public int medium { get; set; }
		public int poor { get; set; }
	}
	public class Transcription {
		public List<Part> parts { get; set; }
		public string quality { get; set; }
		public QualityCounts qualityCounts { get; set; }
	}
	public class Root {
		public string _id { get; set; }
		public Metadata metadata { get; set; }
		public Transcription transcription { get; set; }
	}
}

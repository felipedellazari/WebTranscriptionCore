using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebTranscriptionCore {
	[DisplayName("Dígitro")]
	public class DigitroProvider : BaseClass, IAutoTranscProvider {
		private readonly TimeSpan MaxAudioDuration = new TimeSpan(2, 0, 0);
		private string UrlBase { get; set; } = null;
		private Uri Url => new Uri(cfgs.Global_Others_OnlineUrl);
		public bool IsEnabled => true;
		public AEngineStatus IsOnline => GetEngineStatus();
		public DigitroProvider(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
		private AEngineStatus GetEngineStatus() {
			if (IsEnabled) {
				try {
					Ping();
					return new OnlineEngineStatus { Status = EngineStatusEnum.Online };
				} catch (Exception ex) {
					return new OnlineEngineStatus { Status = EngineStatusEnum.Unkown, MessageStatus = GetErrorMessage(ex) };
				}
			}
			return new OnlineEngineStatus { Status = EngineStatusEnum.Unkown };
		}
		private string GetErrorMessage(Exception ex) => ex.InnerException != null ? GetErrorMessage(ex.InnerException) : ex.Message;
		private void Ping() {
			using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
				using (HttpResponseMessage response = client.GetAsync(new Uri(Url, $"Digitro/Ping")).Result) {
					response.EnsureSuccessStatusCode();
					if (response.Content.ReadAsStringAsync().Result != @"""Running""")
						throw new Exception("Result doesnt match.");
				}
			}
		}
		public CreateJobResult CreateTranscriptionJob(long taskId, string mp3File, TimeSpan totalAudioDuration) {
			HttpWebResponse respAudio = null;
			try {
				//SessionTask task = BLL.SessionTasks.Load(taskId);
				//Log.Progress($"Enviando tarefa '{ task }' para Transcrição automática...");
				//Session session = BLL.Sessions.LoadByTaskId(taskId);
				//int audioDuration = (int)MP3File.GetFileDuration(mp3File).TotalSeconds;
				TranscriptionMessage createNewResp;
				Guid guid = new Guid(812576439, 6723, 3135, BitConverter.GetBytes(taskId));

				//Cria transcrição.
				using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginAPI());
					client.Timeout = TimeSpan.FromMilliseconds(-1);
					using (HttpResponseMessage respNew = client.GetAsync(new Uri(Url, $"Digitro/CreateNew/{cfg.GetSection("Auto_Transc_Serial").Value}/{guid}/{Convert.ToInt32(totalAudioDuration.TotalSeconds)}")).Result) {
						respNew.EnsureSuccessStatusCode();
						Task<string> resultNew = respNew.Content.ReadAsStringAsync();
						createNewResp = JsonConvert.DeserializeObject<TranscriptionMessage>(resultNew.Result);
						if (createNewResp.Message != null) throw new Exception(createNewResp.Message);
					}
				}

				//Envia arquivo de áudio.
				Uri url = new Uri(Url, $"Digitro/SendTranscription/{createNewResp.TranscriptionId}/{Convert.ToInt32(totalAudioDuration.TotalSeconds)}");
				HttpWebRequest reqAudio = (HttpWebRequest)WebRequest.Create(url);
				reqAudio.Accept = "application/octet-stream";
				reqAudio.Method = "POST";
				reqAudio.KeepAlive = true;
				reqAudio.Timeout = Timeout.Infinite;
				reqAudio.MaximumResponseHeadersLength = int.MaxValue;
				reqAudio.ReadWriteTimeout = Timeout.Infinite;
				reqAudio.Headers.Add("User", cfgs.DigitroUser);
				reqAudio.Headers.Add("Password", cfgs.DigitroPassword);
				using (Stream fileStream = File.OpenRead(mp3File))
				using (Stream requestStream = reqAudio.GetRequestStream()) {
					int bufferSize = 1024;
					byte[] buffer = new byte[bufferSize];
					int byteCount = 0;
					while ((byteCount = fileStream.Read(buffer, 0, bufferSize)) > 0) requestStream.Write(buffer, 0, byteCount);
					requestStream.Flush();
					requestStream.Close();
				}
				respAudio = (HttpWebResponse)reqAudio.GetResponse();
				if (respAudio.StatusCode != HttpStatusCode.OK)
					throw new Exception($"Falha ao enviar arquivo de áudio para Transcrição automática Dígitro. Servidor retornou status StatusCode: { respAudio.StatusCode }. Tarefa: {taskId }.");

				// Salva o histórico da transcrição para geração de relatórios
				var transcriptionTask = SessionTask.GetById(cfg, claims, taskId);
				if (transcriptionTask == null) throw new Exception($"Erro na conversão da task {taskId}.");
				DateTime dateSend = DateTime.Now;
				DateTime? dateReceived = null;
				long userId = BLL.CurrentUserId(claims);
				TimeSpan duration = totalAudioDuration;
				TimeSpan extraTimeTotal = totalAudioDuration - transcriptionTask.Session.TurnDuration < TimeSpan.FromSeconds(0) ? TimeSpan.FromSeconds(0) : totalAudioDuration - transcriptionTask.Session.TurnDuration;
				int status = 0;
				string licenceSerial = cfg.GetSection("Auto_Transc_Serial").Value;
				int providerId = 2;
				new AutoTranscriptionHistory(cfg).Set(taskId, guid, dateSend, dateReceived, userId, duration, extraTimeTotal, status, licenceSerial, providerId);
				return new CreateJobResult(CreateJobStatus.Success) { Reference = createNewResp.TranscriptionId.ToString() };
			} catch (Exception ex) {
				return new CreateJobResult(CreateJobStatus.Error, GetErrorMessage(ex));
			} finally {
				respAudio?.Close();
				File.Delete(mp3File);
			}
		}
		private string LoginAPI() {
				try {
					using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
						Login login = new Login { SerialNumber = cfg.GetSection("Auto_Transc_Serial").Value };
						using (StringContent sc = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json")) {
							using (HttpResponseMessage response = client.PostAsync(new Uri(Url, "Login"), sc).Result) {
								response.EnsureSuccessStatusCode();
								if (response.IsSuccessStatusCode) {
									string result = response.Content.ReadAsStringAsync().Result;
									Login model = JsonConvert.DeserializeObject<Login>(result);
									if (model.authenticated) {
										return model.accessToken;
									}
								}
							}
						}
					}
				} catch (Exception ex) {
					throw new ValidationException($"Problemas ao fazer o Login na API Dígitro: ({GetErrorMessage(ex)})");
				}
			throw new ValidationException("Falha na autenticação do serviço de Transcrição automática");
		}
		public void DeleteTranscriptionJob(string transcriptionId) => throw new NotImplementedException();
		public JobStatus GetTranscriptionStatus(string transcriptionId) => GetDetails(transcriptionId, false).Status;
		public IJobDetails GetTranscriptionJobDetails(string transcriptionId) => GetDetails(transcriptionId, true);
		private IJobDetails GetDetails(string transcriptionId, bool getText) {
			if (!IsEnabled) return new JobDetails();
			JobDetails result = new JobDetails() { IsOnline = IsOnline };
			if (!result.IsOnline) return result;
			if (string.IsNullOrEmpty(transcriptionId)) {
				result.Status = JobStatus.NotFound;
				return result;
			}

			//Busca o status.
			try {
				using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
					client.BaseAddress = Url;
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginAPI());
					client.DefaultRequestHeaders.Add("User", cfgs.DigitroUser);
					client.DefaultRequestHeaders.Add("Password", cfgs.DigitroPassword);
					client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
					using (HttpResponseMessage resp = client.GetAsync($"Digitro/GetStatusTranscriptionDigitro/{transcriptionId}").Result) {
						resp.EnsureSuccessStatusCode();
						Task<string> cont = resp.Content.ReadAsStringAsync();
						ReturnStatusInformation transc = JsonConvert.DeserializeObject<ReturnStatusInformation>(cont.Result);
						transc.GetHashCode();
						if (transc.status == TranscriptionStatus.Waiting || transc.status == TranscriptionStatus.Receiving || transc.status == TranscriptionStatus.Pending)
							result.Status = JobStatus.Queued;
						else if (transc.status == TranscriptionStatus.Transcribing || transc.status == TranscriptionStatus.Splitting || transc.status == TranscriptionStatus.Reviewing)
							result.Status = JobStatus.Transcribing;
						else if (transc.status == TranscriptionStatus.Completed) {
							result.Status = JobStatus.Transcribed;

							//busca o texto.
							if (getText) {
								using (HttpClient client2 = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
									client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginAPI());
									client2.DefaultRequestHeaders.Add("User", cfgs.DigitroUser);
									client2.DefaultRequestHeaders.Add("Password", cfgs.DigitroPassword);
									client2.Timeout = TimeSpan.FromMilliseconds(-1);
									Task<HttpResponseMessage> task = client2.PostAsync(new Uri(Url, $"Digitro/AddTranscription/{transcriptionId}"), null);
									task.Wait();
									HttpResponseMessage resp2 = task.Result;
									resp2.EnsureSuccessStatusCode();
									Task<string> ret2 = resp2.Content.ReadAsStringAsync();
									TranscResult res2 = JsonConvert.DeserializeObject<TranscResult>(ret2.Result);
									result.Transcription = GetPlainText(res2.TextInfo);
								}
							}

						} else if (transc.status == TranscriptionStatus.Error)
							result.Status = JobStatus.Failed;
						else
							result.Status = JobStatus.Unkown;
						return result;
					}
				}
			} catch (Exception ex) {
				result.Status = JobStatus.Failed;
				result.Message = $"Ocorreu um erro ao obter os detalhes da transcrição Dígitro: {ex.Message}";
				return result;
			}
		}
		public string GetPlainText(string text) {
			string plainText = "";
			Root root = JsonConvert.DeserializeObject<Root>(text);
			List<string> parts = root?.transcription?.parts?.Select(a => a.displaytext).ToList();
			parts?.ForEach(txt => plainText += $"{txt}{(cfgs.Digitro_NewLine.ToLower() == "true" ? "<br />" : "")}");
			return plainText.Cleanup() ?? " ";
		}

		private class Login {
				public bool authenticated { get; set; }
				public DateTime created { get; set; }
				public DateTime expiration { get; set; }
				public string accessToken { get; set; }
				public string message { get; set; }

				public string SerialNumber { get; set; }
				public string AccessKey { get; set; }
		}

		private class TranscriptionMessage {
			public int TranscriptionId { get; set; }
			public string Message { get; set; }
		}

		private class ReturnStatusInformation {
			[JsonProperty("id")]
			public string id { get; set; }
			[JsonProperty("name")]
			public string name { get; set; }
			[JsonProperty("duration")]
			public int duration { get; set; }
			[JsonProperty("description")]
			public string description { get; set; }
			[JsonProperty("status")]
			public TranscriptionStatus status { get; set; }
		}

		private enum TranscriptionStatus {
			[Description("waiting")] Waiting,
			[Description("receiving")] Receiving,
			[Description("pending")] Pending,
			[Description("transcribing")] Transcribing,
			[Description("splitting")] Splitting,
			[Description("reviewing")] Reviewing,
			[Description("completed")] Completed,
			[Description("error")] Error
		}

		private class TranscResult {
			public string Text { get; set; }
			public string TextInfo { get; set; }
			public string TextError { get; set; }
		}
		private class OnlineEngineStatus : AEngineStatus {
			public override EngineStatusEnum Status { get; set; } = EngineStatusEnum.Unkown;
		}
	}
}

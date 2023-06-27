using FlacLibSharp;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebTranscriptionCore {
	[DisplayName("Online Provider")]
	public class OnlineProvider : BaseClass, IAutoTranscProvider {
		private readonly TimeSpan MaxAudioDuration = new TimeSpan(2, 0, 0);
		private static readonly int sampleRateHertz = 16000;
		public bool IsEnabled => true;
		private Uri Url => new Uri(cfgs.Global_Others_OnlineUrl);
		public AEngineStatus IsOnline => GetEngineStatus();
		private string Token { get; set; }
		public OnlineProvider(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
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
				using (HttpResponseMessage response = client.GetAsync(new Uri(Url, $"Transcription/Ping")).Result) {
					response.EnsureSuccessStatusCode();
				}
			}
		}

		public CreateJobResult CreateTranscriptionJob(long taskId, string mp3File, TimeSpan totalAudioDuration) {
			try {
				Guid guid = new Guid(812576439, 6723, 3135, BitConverter.GetBytes(taskId));
				using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginAPI());
					client.Timeout = TimeSpan.FromMilliseconds(-1);
					using (HttpResponseMessage response = client.GetAsync(new Uri(Url, $"Transcription/CreateNew/{cfg.GetSection("Auto_Transc_Serial").Value}/{guid}/{Convert.ToInt32(totalAudioDuration.TotalSeconds)}")).Result) {
						response.EnsureSuccessStatusCode();
						Task<string> result = response.Content.ReadAsStringAsync();
						int transcriptionId = Convert.ToInt32(result.Result);
						switch (transcriptionId) {
							case int id when transcriptionId > 0:
								PrepareAudioFiles(transcriptionId, guid, mp3File, totalAudioDuration);

								// Salva o histórico da transcrição para geração de relatórios
								var transcriptionTask = SessionTask.GetById(cfg, claims, taskId);
								DateTime dateSend = DateTime.Now;
								DateTime? dateReceived = null;
								long userId = BLL.CurrentUserId(claims);
								TimeSpan duration = totalAudioDuration;
								TimeSpan extraTimeTotal = totalAudioDuration - transcriptionTask.Session.TurnDuration < TimeSpan.FromSeconds(0) ? TimeSpan.FromSeconds(0) : totalAudioDuration - transcriptionTask.Session.TurnDuration;
								int status = 0;
								string licenceSerial = cfg.GetSection("Auto_Transc_Serial").Value;
								int providerId = 1;
								new AutoTranscriptionHistory(cfg).Set(taskId, guid, dateSend, dateReceived, userId, duration, extraTimeTotal, status, licenceSerial, providerId);
								return new CreateJobResult(CreateJobStatus.Success) { Reference = transcriptionId.ToString() };
							case -2:
								throw new Exception($"Franquia excedida. Código { transcriptionId }.");
							default:
								throw new Exception($"Erro { transcriptionId }.");
						}
					}
				}
			} catch (Exception ex) {
				return new CreateJobResult(CreateJobStatus.Error, GetErrorMessage(ex));
			} finally {
				File.Delete(mp3File);
			}
		}

		private void PrepareAudioFiles(int transcriptionId, Guid guid, string file, TimeSpan fileDuration) {
			List<Task> tasks = new List<Task>();
			int fileOrder = 1;
			if (fileDuration > MaxAudioDuration) {
				int count = (int)Math.Ceiling((double)fileDuration.Ticks / MaxAudioDuration.Ticks);
				TimeSpan minuteInitial = TimeSpan.MinValue;
				TimeSpan minuteFinal = MaxAudioDuration;
				for (int i = 1; i <= count; i++) {
					string newFile = Path.GetTempFileName();
					BLL.SliceFile(file, newFile, minuteInitial.TotalSeconds, minuteFinal.TotalSeconds - minuteInitial.TotalSeconds, claims);
					tasks.Add(new Task(() => { SendAndConfirmAudioFile(transcriptionId, guid, newFile, Convert.ToDecimal($"{fileOrder},{i:00}")); }));
					minuteInitial += MaxAudioDuration;
					minuteFinal += MaxAudioDuration;
				}
				File.Delete(file);
			} else {
				tasks.Add(new Task(() => { SendAndConfirmAudioFile(transcriptionId, guid, file, fileOrder); }));
			}
			fileOrder++;
			tasks.ForEach(t => t.Start());
			Task.WaitAll(tasks.ToArray());
		}

		private async void SendAndConfirmAudioFile(int transcriptionId, Guid guid, string input, decimal fileOrder) {
			string output = Path.ChangeExtension(input, ".flac");
			try {
				BLL.Obliterate(output);
				using (Process proc = new Process {
					StartInfo = new ProcessStartInfo(BLL.FFmpeg(claims)) {
						Arguments = $"-i \"{input}\" -vn -filter_complex \"[0:a]amix = inputs = 1[mixed]\" -map \"[mixed]\" -b:a 64k -ar 16000 -sample_fmt s16 -c:a flac \"{ output }\""
					}
				}) {
					proc.Start();
					proc.WaitForExit((int)new TimeSpan(0, 10, 0).TotalMilliseconds);
				};
				int audioDuration = 0;
				int Channels = 0;
				using (FlacFile file = new FlacFile(output)) {
					audioDuration = file.StreamInfo.Duration;
					Channels = file.StreamInfo.Channels;
				}

				var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
				httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

				using (HttpClient client = new HttpClient(httpClientHandler) { Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite) }) {
					using (MultipartFormDataContent formData = new MultipartFormDataContent()) {
						using (Stream s = File.OpenRead(output)) {
							using (StreamContent sc = new StreamContent(s)) {
								formData.Add(sc, Path.GetFileName(output), output);
								HttpResponseMessage resp = await client.PostAsync(new Uri(Url, $"Transcription/AddAudio?TranscriptionId={transcriptionId}"), formData);
								resp.EnsureSuccessStatusCode();
								TranscriptionHistoricVM model = JsonConvert.DeserializeObject<TranscriptionHistoricVM>(resp.Content.ReadAsStringAsync().Result);
								resp.Dispose();
								model.AudioDuration = audioDuration;
								model.FileOrder = fileOrder;
								model.TranscriptionId = transcriptionId;
								model.GuidProduct = guid;
								model.SampleRateHertz = sampleRateHertz;
								model.Channels = Channels;

								var httpClientHandler2 = new HttpClientHandler() { UseDefaultCredentials = true };
								httpClientHandler2.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

								using (HttpClient client2 = new HttpClient(httpClientHandler2)) {
									client2.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
									using (StringContent sc2 = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json")) {
										HttpResponseMessage resp2 = await client2.PostAsync(new Uri(Url, "Transcription/SendTranscription"), sc2);
										resp2.Dispose();
									}
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				throw ex;
			} finally {
				BLL.Obliterate(input);
				BLL.Obliterate(output);
			}
		}

		public IJobDetails GetTranscriptionJobDetails(string transcriptionId) {
			if (!IsEnabled) return new JobDetails();
			JobDetails result = new JobDetails() { IsOnline = IsOnline };
			if (!result.IsOnline) return result;
			if (string.IsNullOrEmpty(transcriptionId)) {
				result.Status = JobStatus.NotFound;
				return result;
			}
			try {
				using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
					client.BaseAddress = Url;
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginAPI());
					client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
					using (HttpResponseMessage response = client.GetAsync($"Transcription/GetTranscription/{transcriptionId}").Result) {
						response.EnsureSuccessStatusCode();
						string ret = response.Content.ReadAsStringAsync().Result;
						TranscriptionHistoricVM thVM = JsonConvert.DeserializeObject<TranscriptionHistoricVM>(ret);
						if (thVM != null) {
							if (!string.IsNullOrEmpty(thVM.Erro)) {
								result.Status = JobStatus.Failed;
								result.Message = $"Ocorreu um erro durante a transcrição. ({thVM.Erro})";
							} else {
								if (thVM.Finished) {
									result.Status = JobStatus.Transcribed;
									result.Transcription = thVM.TranscriptionText;
								} else {
									result.Status = JobStatus.Transcribing;
									result.Message = "A transcrição já foi iniciada e deve terminar em breve.";
								}
							}
						} else {
							result.Status = JobStatus.Failed;
							result.Message = "Ocorreu um erro durante a transcrição.";
						}
						result.DateEnd = thVM.EndTime;
						return result;
					}
				}
			} catch (Exception ex) {
				result.Status = JobStatus.Failed;
				result.Message = "Ocorreu um erro durante a transcrição." + Environment.NewLine + ex.Message;
				return result;
			}
		}
		private string LoginAPI() {
			try {
				using (HttpClient client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })) {
					LoginVM login = new LoginVM { SerialNumber = cfg.GetSection("Auto_Transc_Serial").Value };
					using (StringContent sc = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json")) {
						using (HttpResponseMessage response = client.PostAsync(new Uri(Url, "Login"), sc).Result) {
							response.EnsureSuccessStatusCode();
							if (response.IsSuccessStatusCode) {
								string result = response.Content.ReadAsStringAsync().Result;
								LoginVM model = JsonConvert.DeserializeObject<LoginVM>(result);
								if (model.authenticated) {
									return Token = model.accessToken;
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				throw new ValidationException($"Problemas ao fazer o Login no serviço de Transcrição automática: ({GetErrorMessage(ex)})");
			}
			throw new ValidationException("Falha na autenticação do serviço de Transcrição automática");
		}

		public class Cfg {
			private readonly NameValueCollection _configs;
			public Cfg(NameValueCollection configs) => _configs = configs;
			public string Cfg_OnlineUrl {
				get => _configs["OnlineUrl"];
				set => _configs["OnlineUrl"] = value;
			}
		}
		private class LoginVM {
			public bool authenticated { get; set; }
			public DateTime created { get; set; }
			public DateTime expiration { get; set; }
			public string accessToken { get; set; }
			public string message { get; set; }

			public string SerialNumber { get; set; }
			public string AccessKey { get; set; }
		}
		private class TranscriptionHistoricVM {
			public int Id { get; set; }
			public int TranscriptionId { get; set; }
			public Guid GuidProduct { get; set; }
			public string FileName { get; set; }
			public decimal FileOrder { get; set; }
			public int AudioDuration { get; set; }
			public DateTime? StartTime { get; set; }
			public DateTime? EndTime { get; set; }
			public string Erro { get; set; }
			public string TranscriptionText { get; set; }
			public bool Finished { get; set; }
			public int TranscriptionIdPrevious { get; set; }
			public int? SampleRateHertz { get; set; }
			public int? Channels { get; set; }

		}
		private class OnlineEngineStatus : AEngineStatus {
			public override EngineStatusEnum Status { get; set; } = EngineStatusEnum.Unkown;
		}
	}
}
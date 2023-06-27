using Microsoft.Extensions.Configuration;
using Microsoft.MediaFoundation;
using Microsoft.MediaFoundation.Misc;
using Microsoft.MediaFoundation.ReadWrite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Timers;

namespace WebTranscriptionCore {

	public static class MediaHelper {
		private const int Ok = 0;
		public static readonly byte[] MP4_HEADER_OBJECT = { 0x66, 0x74, 0x79, 0x70 };
		public delegate void ProgressCallback2(double percent);
		public enum eAVEncH264VProfile {
			eAVEncH264VProfile_unknown = 0,
			eAVEncH264VProfile_Simple = 66,
			eAVEncH264VProfile_Base = 66,
			eAVEncH264VProfile_Main = 77,
			eAVEncH264VProfile_High = 100,
			eAVEncH264VProfile_422 = 122,
			eAVEncH264VProfile_High10 = 110,
			eAVEncH264VProfile_444 = 144,
			eAVEncH264VProfile_Extended = 88
		};

		public static bool Is(string fileName) {
			FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			try {
				return Is(fs);
			} finally {
				fs.Dispose();
			}
		}

		public static bool Is(FileStream stream) {
			byte[] b = new byte[8];
			long pos = 0;
			if (stream.CanSeek) pos = stream.Position;
			int sz = stream.Read(b, 0, b.Length);
			if (stream.CanSeek) stream.Position = pos;
			if (sz != b.Length) return false;
			byte[] header = new byte[4];
			Array.Copy(b, 4, header, 0, 4);
			return header.SequenceEqual(MP4_HEADER_OBJECT);
		}

		public static TimeSpan GetFileDuration(string fileName, bool precise = false) {
			MFExtern.StartMF();
			int hr;
			TimeSpan ts;

			uint MF_SOURCE_READER_FIRST_AUDIO_STREAM = 0xFFFFFFFD;
			uint MF_SOURCE_READER_ALL_STREAMS = 0xFFFFFFFE;

			MFExtern.MFCreateSourceReaderFromURL(fileName, null, out IMFSourceReader reader);

			hr = reader.SetStreamSelection((int)MF_SOURCE_READER_ALL_STREAMS, false);
			hr = reader.SetStreamSelection((int)MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);

			if (reader == null) return TimeSpan.Zero;

			long totalTicks = 0;
			while (true) {
				hr = reader.ReadSample((int)MF_SOURCE_READER_FIRST_AUDIO_STREAM, MF_SOURCE_READER_CONTROL_FLAG.None, out int index, out MF_SOURCE_READER_FLAG flag, out long ticks, out IMFSample sample);

				if (flag == MF_SOURCE_READER_FLAG.EndOfStream) break;
				hr = sample.GetSampleDuration(out ticks);
				if (hr == MFError.MF_E_NO_SAMPLE_DURATION) goto free;
				Marshal.ThrowExceptionForHR(hr);
				totalTicks += ticks;

			free:
				if (sample != null) {
					Marshal.ReleaseComObject(sample);
					sample = null;
				}
			}

			Marshal.ReleaseComObject(reader);
			ts = TimeSpan.FromTicks(totalTicks);
			MFExtern.StopMF();
			return ts;
		}

		public static TimeSpan MergeFiles(string[] inputFiles, string outputFile, ProgressCallback2 onProgress) {
			uint MF_SOURCE_READER_FIRST_AUDIO_STREAM = 0xFFFFFFFD;
			uint MF_SOURCE_READER_ALL_STREAMS = 0xFFFFFFFE;

			if (inputFiles.Length == 0) throw new ArgumentOutOfRangeException("no files to merge");

			float[] filePct = inputFiles.Select(x => (float)new FileInfo(x).Length).ToArray();
			float total = filePct.Sum();

			for (int i = 0; i < filePct.Length; i++) filePct[i] = filePct[i] / total;

			IMFSourceReader reader = null;
			IMFSinkWriter writter = null;
			IMFMediaType videoMediaType = null;
			IMFMediaType audioMediaType = null;
			IMFSample sample = null;
			int str = 0;
			int hr = Ok;
			long time;
			int prog = 0;

			MFExtern.StartMF();
			PropVariant startOffset = new PropVariant(0L);

			MFExtern.MFCreateAttributes(out IMFAttributes readerAttributes, 1);
			readerAttributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);

			MFExtern.MFCreateAttributes(out IMFAttributes writterAttributes, 1);
			writterAttributes.SetUINT32(MFAttributesClsid.MF_SINK_WRITER_DISABLE_THROTTLING, 1);

			long result = 0;
			{
				hr = MFExtern.MFCreateSourceReaderFromURL(inputFiles[0], readerAttributes, out reader);
				hr = reader.GetNativeMediaType(0, 0, out audioMediaType);
				hr = reader.GetNativeMediaType(1, 0, out videoMediaType);

				hr = MFExtern.MFCreateFile(MFFileAccessMode.Write, MFFileOpenMode.DeleteIfExist, MFFileFlags.None, outputFile, out IMFByteStream stream);
				hr = MFExtern.MFCreateMPEG4MediaSink(stream, videoMediaType, audioMediaType, out IMFMediaSink mediaSink);
				hr = MFExtern.MFCreateSinkWriterFromMediaSink(mediaSink, writterAttributes, out writter);


				float done = 0;
				writter.BeginWriting();

				for (int i = 0; i < inputFiles.Length;) {
					hr = reader.SetStreamSelection((int)MF_SOURCE_READER_ALL_STREAMS, false);
					hr = reader.SetStreamSelection((int)MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);
					hr = reader.SetCurrentMediaType(0, null, audioMediaType);
					hr = reader.SetCurrentPosition(Guid.Empty, startOffset);
					long max;
					max = 0;

					hr = reader.ReadSample((int)MF_SOURCE_READER_FIRST_AUDIO_STREAM, MF_SOURCE_READER_CONTROL_FLAG.None, out int audio, out MF_SOURCE_READER_FLAG flags, out long min, out sample);
					Marshal.ThrowExceptionForHR(hr);
					Marshal.ReleaseComObject(sample);

					while (true) {
						hr = reader.ReadSample((int)MF_SOURCE_READER_FIRST_AUDIO_STREAM, MF_SOURCE_READER_CONTROL_FLAG.None, out audio, out flags, out time, out sample);
						if (flags == MF_SOURCE_READER_FLAG.EndOfStream) break;
						Marshal.ThrowExceptionForHR(hr);
						hr = sample.GetSampleDuration(out long dur);
						Marshal.ThrowExceptionForHR(hr);
						max = time + dur;
					}

					hr = reader.SetStreamSelection((int)MF_SOURCE_READER_ALL_STREAMS, false);
					hr = reader.SetStreamSelection((int)MF_SOURCE_READER_ALL_STREAMS, true);
					hr = reader.SetCurrentMediaType(1, null, videoMediaType);
					hr = reader.SetCurrentMediaType(0, null, audioMediaType);
					hr = reader.SetCurrentPosition(Guid.Empty, startOffset);

					while (true) {
						sample = null;
						hr = reader.ReadSample((int)MF_SOURCE_READER_ALL_STREAMS, MF_SOURCE_READER_CONTROL_FLAG.None, out str, out flags, out time, out sample);
						if (flags == MF_SOURCE_READER_FLAG.EndOfStream) break;
						Marshal.ThrowExceptionForHR(hr);

						if (str != audio) {
							if (time >= max) {
								Marshal.ReleaseComObject(sample);
								continue;
							}
						}

						hr = sample.SetSampleTime(result + time);
						hr = writter.WriteSample((str == audio) ? 1 : 0, sample);
						Marshal.ThrowExceptionForHR(hr);
						hr = MFExtern.MFShutdownObject(sample);
						hr = Marshal.ReleaseComObject(sample);

						if (onProgress != null && ((prog++ & 1023) == 0)) {
							onProgress((Single)time / max * filePct[i] + done);
						}
					}

					result += max - min;
					done += filePct[i];
					Marshal.ReleaseComObject(reader);
					if (++i == inputFiles.Length) break;
					hr = MFExtern.MFCreateSourceReaderFromURL(inputFiles[i], readerAttributes, out reader);

				}


				writter.NotifyEndOfSegment(0);
				writter.Finalize_();
				Marshal.ReleaseComObject(writter);
				mediaSink.Shutdown();
				Marshal.ReleaseComObject(mediaSink);
				GC.Collect();
				Marshal.CleanupUnusedObjectsInCurrentContext();
				MFExtern.StopMF();
				return new TimeSpan(result);
			}
		}

		public static void ConvertToMP4(string srcFile, string dstFile, Action callback, ProgressCallback2 progress = null) {
			MFExtern.StartMF();
			EncodingSessionController session = new EncodingSessionController(srcFile, dstFile, progress) {
				Finished = () => { MFExtern.StopMF(); }
			};
			if (callback != null) session.Finished += callback;

			session.Start();
		}
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		private class EncodingSessionController : IMFAsyncCallback {
			private Timer progressTimer;
			private IMFMediaSession session;
			private IMFMediaSource mediaSrc;
			private IMFByteStream bs;
			private IMFPresentationClock presentationClock;
			private string srcFile, dstFile;
			public Action Finished;
			private ProgressCallback2 OnProgress;
			private double duration;
			private double timeElapsed = 0;

			private void DoProgress(double progress) {
				this.OnProgress?.Invoke(progress);
			}

			public EncodingSessionController(string srcFile, string dstFile, ProgressCallback2 progress = null) {

				this.OnProgress = progress;
				this.srcFile = srcFile;
				this.dstFile = dstFile;
				this.progressTimer = new Timer(500);
				this.progressTimer.Elapsed += progressTimer_Elapsed;
			}

			void progressTimer_Elapsed(object sender, ElapsedEventArgs e) {
				if (this.presentationClock == null) return;
				long time;
				this.presentationClock.GetTime(out time);
				double elapsed = Convert.ToDouble(time) / this.duration;
				if (elapsed.Equals(this.timeElapsed)) return;
				this.timeElapsed = elapsed;
				this.DoProgress(elapsed);
			}

			public int GetParameters(out MFASync pdwFlags, out MFAsyncCallbackQueue pdwQueue) {
				pdwFlags = MFASync.FastIOProcessingCallback;
				pdwQueue = MFAsyncCallbackQueue.Standard;
				return Ok;
			}

			public int Invoke(IMFAsyncResult pAsyncResult) {
				int hr;
				hr = session.EndGetEvent(pAsyncResult, out IMFMediaEvent mediaEvent);
				if (hr < 0) goto done;

				hr = mediaEvent.GetType(out MediaEventType eventType);
				if (hr < 0) goto done;
				hr = mediaEvent.GetStatus(out int hrStatus);
				if (hr < 0) goto done;
				if (hrStatus < 0) {
					hr = hrStatus;
					goto done;
				}

				switch (eventType) {
					case MediaEventType.MESessionTopologyStatus:
						int i;
						hr = mediaEvent.GetUINT32(MFAttributesClsid.MF_EVENT_TOPOLOGY_STATUS, out i);
						MFError.ThrowExceptionForHR(hr);
						MFTopoStatus TopoStatus = (MFTopoStatus)i;
						if (TopoStatus == MFTopoStatus.Ready) hr = session.Start(Guid.Empty, new PropVariant());
						break;

					case MediaEventType.MESessionStarted:
						IMFClock clock = null;
						this.session.GetClock(out clock);
						this.presentationClock = (IMFPresentationClock)clock;
						this.progressTimer.Start();
						break;

					case MediaEventType.MESessionEnded:
						hr = session.Close();
						this.progressTimer.Stop();
						this.DoProgress(1);
						if (hr < 0) goto done;
						break;

					case MediaEventType.MESessionClosed:
						mediaSrc.Shutdown();
						this.bs.Close();
						Marshal.ReleaseComObject(bs);
						session.Shutdown();
						Marshal.ReleaseComObject(mediaSrc);
						Marshal.ReleaseComObject(session);
						if (this.presentationClock != null) {
							Marshal.ReleaseComObject(this.presentationClock);
							this.presentationClock = null;
						}
						session = null;
						Action a = this.Finished;
						if (a != null) a();
						break;
				}

				if (eventType != MediaEventType.MESessionClosed) session.BeginGetEvent(this, null);

				done:

				if (hr < 0) {
					hrStatus = hr;
					session.Close();
				}

				Marshal.ReleaseComObject(mediaEvent);
				return hr;
			}

			private int CalcVideoBitrate(IMFAttributes videoAtts, int motionRank) {
				MFExtern.MFGetAttributeSize(videoAtts, MFAttributesClsid.MF_MT_FRAME_SIZE, out int w, out int h);
				MFExtern.MFGetAttributeRatio(videoAtts, MFAttributesClsid.MF_MT_FRAME_RATE, out int num, out int den);
				int rate = num / den;
				int pixelCount = (w * h);
				int kg = (int)(pixelCount * rate * motionRank * 0.07);
				return kg;
			}

			public void Start() {
				int hr;
				MFObjectType objectType = MFObjectType.Invalid;

				hr = MFExtern.MFCreateFile(MFFileAccessMode.Read, MFFileOpenMode.FailIfNotExist, MFFileFlags.None, srcFile, out bs);
				IMFAttributes fileAtts = (bs as IMFAttributes);
				hr = fileAtts.SetString(MFAttributesClsid.MF_BYTESTREAM_CONTENT_TYPE, ".wmv");

				hr = MFExtern.MFCreateSourceResolver(out IMFSourceResolver srcResolver);
				hr = srcResolver.CreateObjectFromByteStream(bs, null, MFResolution.MediaSource | MFResolution.Read, null, out objectType, out object unkObj);
				Marshal.ThrowExceptionForHR(hr);
				Marshal.ReleaseComObject(srcResolver);

				mediaSrc = (IMFMediaSource)unkObj;
				mediaSrc.CreatePresentationDescriptor(out IMFPresentationDescriptor pd);
				hr = pd.GetUINT64(MFAttributesClsid.MF_PD_DURATION, out long dur);
				this.duration = Convert.ToDouble(dur);
				Marshal.ReleaseComObject(pd);

				MFExtern.MFCreateAttributes(out IMFAttributes mediaSrcAtts, 1);
				mediaSrcAtts.SetUINT32(MFAttributesClsid.MF_SOURCE_READER_DISCONNECT_MEDIASOURCE_ON_SHUTDOWN, 1);
				hr = MFExtern.MFCreateSourceReaderFromMediaSource(mediaSrc, mediaSrcAtts, out IMFSourceReader reader);
				hr = Marshal.ReleaseComObject(mediaSrcAtts);

				IMFAttributes videoAtts = null;
				IMFAttributes audioAtts = null;

				hr = MFExtern.MFCreateTranscodeProfile(out IMFTranscodeProfile profile);
				hr = MFExtern.MFCreateAttributes(out IMFAttributes containerAttrs, 1);
				hr = containerAttrs.SetGUID(MFAttributesClsid.MF_TRANSCODE_CONTAINERTYPE, MFTranscodeContainerType.MPEG4);
				int i = 0;

				while (true) {

					hr = reader.GetNativeMediaType(i, 0, out IMFMediaType mediaType);
					if (hr == MFError.MF_E_INVALIDSTREAMNUMBER) break;
					mediaType.GetMajorType(out Guid guidMajorType);
					if ((guidMajorType.Equals(MFMediaType.Video)) && videoAtts == null) {
						hr = MFExtern.MFCreateAttributes(out videoAtts, 5);
						hr = videoAtts.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.H264);
						hr = videoAtts.SetUINT32(MFAttributesClsid.MF_MT_MPEG2_PROFILE, (int)eAVEncH264VProfile.eAVEncH264VProfile_Base);
						hr = mediaType.CopyAttributeTo(videoAtts, MFAttributesClsid.MF_MT_FRAME_SIZE);
						hr = mediaType.CopyAttributeTo(videoAtts, MFAttributesClsid.MF_MT_FRAME_RATE);

						hr = mediaType.CopyAttributeTo(videoAtts, MFAttributesClsid.MF_MT_AVG_BITRATE);

						int motionRank = 1;
						int kg = CalcVideoBitrate(videoAtts, motionRank);
						videoAtts.SetUINT32(MFAttributesClsid.MF_MT_AVG_BITRATE, kg);
						hr = profile.SetVideoAttributes(videoAtts);

						Marshal.ReleaseComObject(videoAtts);
					}

					if ((guidMajorType.Equals(MFMediaType.Audio)) && audioAtts == null) {
						hr = MFExtern.MFCreateAttributes(out audioAtts, 7);
						hr = audioAtts.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.AAC);
						hr = mediaType.CopyAttributeTo(audioAtts, MFAttributesClsid.MF_MT_AUDIO_NUM_CHANNELS);
						hr = audioAtts.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_BITS_PER_SAMPLE, 16);
						hr = audioAtts.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_SAMPLES_PER_SECOND, 44100);
						hr = audioAtts.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_BLOCK_ALIGNMENT, 1);
						hr = audioAtts.SetUINT32(MFAttributesClsid.MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION, 0x29);
						hr = audioAtts.SetUINT32(MFAttributesClsid.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 12000);
						hr = profile.SetAudioAttributes(audioAtts);
						Marshal.ReleaseComObject(audioAtts);
					}
					Marshal.ReleaseComObject(mediaType);
					i++;
				}

				Marshal.ReleaseComObject(reader);

				hr = profile.SetContainerAttributes(containerAttrs);
				hr = Marshal.ReleaseComObject(containerAttrs);

				hr = MFExtern.MFCreateTranscodeTopology(mediaSrc, dstFile, profile, out IMFTopology topology);
				Marshal.ReleaseComObject(profile);

				hr = MFExtern.MFCreateMediaSession(null, out this.session);
				hr = this.session.BeginGetEvent(this, null);
				hr = session.SetTopology(MFSessionSetTopologyFlags.Immediate, topology);
				Marshal.ReleaseComObject(topology);
			}
		}
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		internal static int CopyAttributeTo(this IMFAttributes source, IMFAttributes dest, Guid guid) {
			int hr = Ok;
			PropVariant p = new PropVariant();
			do {
				hr = source.GetItem(guid, p);
				if (hr < 0) break;
				hr = dest.SetItem(guid, p);
				if (hr < 0) break;
			} while (false);
			return hr;
		}

		internal static Size? GetVideoSize(string srcFile) {
			MFExtern.StartMF();
			uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
			MFExtern.MFCreateAttributes(out IMFAttributes attributes, 1);
			MFExtern.MFCreateSourceReaderFromURL(srcFile, attributes, out IMFSourceReader reader);
			reader.GetCurrentMediaType((int)MF_SOURCE_READER_FIRST_VIDEO_STREAM, out IMFMediaType type);
			MFExtern.MFGetAttributeSize(type, MFAttributesClsid.MF_MT_FRAME_SIZE, out int width, out int height);
			Marshal.ReleaseComObject(type);
			Marshal.ReleaseComObject(reader);
			Marshal.ReleaseComObject(attributes);
			MFExtern.StopMF();
			return new Size(width, height);
		}

		internal static bool IsAudioOnly(string srcFile) {
			uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
			uint MF_SOURCE_READER_FIRST_AUDIO_STREAM = 0xFFFFFFFD;
			MFExtern.MFCreateAttributes(out IMFAttributes attributes, 1);
			MFExtern.MFCreateSourceReaderFromURL(srcFile, attributes, out IMFSourceReader reader);
			int hr = reader.GetCurrentMediaType((int)MF_SOURCE_READER_FIRST_VIDEO_STREAM, out IMFMediaType mediaType);
			Marshal.ReleaseComObject(mediaType);
			if (hr == Ok) return false;
			hr = reader.GetCurrentMediaType((int)MF_SOURCE_READER_FIRST_AUDIO_STREAM, out mediaType);
			Marshal.ReleaseComObject(mediaType);
			Marshal.ReleaseComObject(reader);
			Marshal.ReleaseComObject(attributes);
			return (hr == Ok);
		}

		public static Bitmap GetFrameAtOffset(string fileName, TimeSpan offset, out bool isAudioFile) {
			isAudioFile = false;
			uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;

			MFExtern.StartMF();
			MFExtern.MFCreateAttributes(out IMFAttributes readerAttributes, 2);
			readerAttributes.SetUINT32(MFAttributesClsid.MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1);
			readerAttributes.SetString(MFAttributesClsid.MF_BYTESTREAM_CONTENT_TYPE, ".mp4");

			var hr = MFExtern.MFCreateSourceReaderFromURL(fileName, readerAttributes, out IMFSourceReader reader);
			Marshal.ThrowExceptionForHR(hr);

			hr = reader.GetCurrentMediaType((int)MF_SOURCE_READER_FIRST_VIDEO_STREAM, out IMFMediaType currentMediaType);

			if (hr == MFError.MF_E_INVALIDSTREAMNUMBER) {
				Marshal.ReleaseComObject(readerAttributes);
				Marshal.ReleaseComObject(reader);
				isAudioFile = true;
				MFExtern.StopMF();
				return null;
			}

			MFExtern.MFGetAttributeSize(currentMediaType, MFAttributesClsid.MF_MT_FRAME_SIZE, out int width, out int height);

			MFExtern.MFCreateMediaType(out IMFMediaType rgbMediaType);
			rgbMediaType.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video);
			rgbMediaType.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.RGB32);

			reader.SetCurrentMediaType((int)MF_SOURCE_READER_FIRST_VIDEO_STREAM, null, rgbMediaType);
			reader.SetStreamSelection((int)MF_SOURCE_READER_FIRST_VIDEO_STREAM, true);

			hr = reader.SetCurrentPosition(Guid.Empty, new PropVariant(offset.Ticks));
			if (hr == MFError.MF_E_INVALID_POSITION) reader.SetCurrentPosition(Guid.Empty, new PropVariant(0L));

			reader.ReadSample((int)MF_SOURCE_READER_FIRST_VIDEO_STREAM, MF_SOURCE_READER_CONTROL_FLAG.None, out int _, out MF_SOURCE_READER_FLAG _, out long _, out IMFSample ppSample);

			Marshal.ReleaseComObject(readerAttributes);
			Marshal.ReleaseComObject(reader);

			ppSample.ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);
			ppBuffer.Lock(out IntPtr ppbBuffer, out int _, out int _);
			var image = new Bitmap(width, height, PixelFormat.Format32bppRgb);
			var bmpdt = image.LockBits(Rectangle.FromLTRB(0, 0, width, height), ImageLockMode.WriteOnly, image.PixelFormat);
			MFExtern.MFCopyImage(bmpdt.Scan0, width * 4, ppbBuffer, width * 4, width * 4, height);
			image.UnlockBits(bmpdt);

			Marshal.Release(ppbBuffer);
			Marshal.ReleaseComObject(ppBuffer);
			Marshal.ReleaseComObject(ppSample);
			Marshal.ReleaseComObject(rgbMediaType);
			Marshal.ReleaseComObject(currentMediaType);
			MFExtern.StopMF();

			return image;
		}

		public static void SliceFile(string srcFile, string dstFile, TimeSpan from, TimeSpan to, ProgressCallback2 progress = null) {
			MFExtern.StartMF();
			uint MF_SOURCE_READER_ALL_STREAMS = 0xFFFFFFFE;
			int hr;
			int audio = 0;
			int prog = 0;

			MFExtern.MFCreateAttributes(out IMFAttributes readerAttributes, 1);
			readerAttributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);

			MFExtern.MFCreateAttributes(out IMFAttributes writterAttributes, 1);
			writterAttributes.SetUINT32(MFAttributesClsid.MF_SINK_WRITER_DISABLE_THROTTLING, 1);

			{
				hr = MFExtern.MFCreateSourceReaderFromURL(srcFile, readerAttributes, out IMFSourceReader reader);
				IMFMediaType audioMediaType;
				hr = reader.GetNativeMediaType(0, 0, out audioMediaType);
				IMFMediaType videoMediaType;
				hr = reader.GetNativeMediaType(1, 0, out videoMediaType);
				hr = reader.SetStreamSelection((int)MF_SOURCE_READER_ALL_STREAMS, false);
				hr = reader.SetStreamSelection((int)MF_SOURCE_READER_ALL_STREAMS, true);
				hr = reader.SetCurrentMediaType(1, null, videoMediaType);
				hr = reader.SetCurrentMediaType(0, null, audioMediaType);

				hr = MFExtern.MFCreateFile(MFFileAccessMode.Write, MFFileOpenMode.DeleteIfExist, MFFileFlags.None, dstFile, out IMFByteStream stream);
				hr = MFExtern.MFCreateMPEG4MediaSink(stream, videoMediaType, audioMediaType, out IMFMediaSink mediaSink);
				hr = MFExtern.MFCreateSinkWriterFromMediaSink(mediaSink, writterAttributes, out IMFSinkWriter writter);

				hr = reader.SetCurrentPosition(Guid.Empty, new PropVariant(from.Ticks));
				hr = writter.BeginWriting();

				while (true) {
					hr = reader.ReadSample((int)MF_SOURCE_READER_ALL_STREAMS, MF_SOURCE_READER_CONTROL_FLAG.None, out int str, out MF_SOURCE_READER_FLAG flags, out long time, out IMFSample sample);

					if (flags == MF_SOURCE_READER_FLAG.EndOfStream) break;
					Marshal.ThrowExceptionForHR(hr);

					if (time > to.Ticks) {
						MFExtern.MFShutdownObject(sample);
						Marshal.ReleaseComObject(sample);
						if (time > to.Ticks + 30000000) break;
						continue;
					}

					sample.SetSampleTime(time - from.Ticks);
					hr = writter.WriteSample((str == audio) ? 1 : 0, sample);
					Marshal.ThrowExceptionForHR(hr);
					MFExtern.MFShutdownObject(sample);
					Marshal.ReleaseComObject(sample);
					DoProgress(progress, ref prog, time, from, to);
				}

				Marshal.FinalReleaseComObject(reader);
				writter.NotifyEndOfSegment(0);
				writter.Finalize_();
				Marshal.FinalReleaseComObject(writter);
				mediaSink.Shutdown();
				Marshal.FinalReleaseComObject(mediaSink);
			}

			GC.Collect();
			Marshal.CleanupUnusedObjectsInCurrentContext();
			MFExtern.StopMF();
		}

		private static void DoProgress(ProgressCallback2 progress, ref int prog, long time, TimeSpan from, TimeSpan to) {
			if (progress == null) return;
			if ((prog++ & 1023) != 0) return;
			if (time - from.Ticks > 0) progress((Single)(time - from.Ticks) / (to.Ticks - from.Ticks));
		}
	}
}
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Text;

namespace WebTranscriptionCore {
	public class ExceptionPage {

		private static void SegmentFile(ref FileStream file) {
			string ext = DateTime.Today.ToString("' ('dd.MM.yyyy').log'");
			string f1 = file.Name;
			file.Dispose();
			string f2 = Path.ChangeExtension(f1, ext);
			File.Move(f1, f2);
			file = new FileStream(f1, FileMode.Append, FileAccess.Write, FileShare.Read);
		}

		public static string GetPage(Exception ex, IWebHostEnvironment env) {

			if (!(ex is ValidationException)) {
				string fn = Path.Combine(env.WebRootPath, "WebTranscription.log");
				FileStream file = new FileStream(fn, FileMode.Append, FileAccess.Write, FileShare.Read, 8);
				Encoding encode = Encoding.GetEncoding(1252);
				try {
					if (file.Length > (40 * 1024 * 1024)) SegmentFile(ref file);
					file.Write(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + "\r\n", encode);
					file.Write(ex.ToString(), encode);
					file.Write(Encoding.GetEncoding(1252).GetBytes("\r\n\r\n"));
				} catch {
				} finally {
					file.Flush(false);
					file.Dispose();
				}
			}

			string title = ex is ValidationException ? "Validação" : "Erro";
			string description = ex is ValidationException ? ex.Message : ex.ToString();

			return @"
						
						<html lang=""pt-br""><body>


						<link rel=""stylesheet"" href=""css/bootstrap.css"" />
					
					<style>
						.fl {
											float: left;
										}
						.div1 {
											height: 80px;
											width: 100%;
										}
						.divLogo {
											background-color: #3E3E3E;
							height: 80px;
											width: 200px;
											padding: 18px 0px 0px 30px;
										}
						.divTitle {
											padding-left: 20px;
										}
						.div3 {
											width: 100%;
											height: 100%;
											background-color: #F2F1F6;
						}
						.div4 {
											height: 100%;
											background-color: #3E3E3E;
							width: 200px;
										}
						.div5 {
											width: 80%;
											padding-left: 20px;
										}
					</style>
					
					<div class=""fl div1"">
						<div class=""fl divLogo""> <img src = ""images/logo-sidebar.png""> </div>
						<div class=""fl divTitle""> <h1>" + title + @"</h1> </div>
					</div>
					
					<div class=""fl div3"">
						<div class=""fl div4""> </div>
						<div class=""fl div5"">
							<br/>
							<h3>" + description + @"</h3>
							<br/>
						</div>
						<div class=""fl div5"">
							<button type = ""button"" class=""btn btn-primary"" onclick=""window.history.back();"">OK</button>
						</div>
					</div>

					</body></html>

					";
		}
	}
}
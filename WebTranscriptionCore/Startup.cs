using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace WebTranscriptionCore {
	public class Startup {
		public Startup(IConfiguration configuration) => Configuration = configuration;

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services) {
			services.AddControllersWithViews();
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => options.LoginPath = "/account/index");
			services.AddSession(options => { options.IdleTimeout = TimeSpan.FromHours(12); });
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			} else {
				app.UseExceptionHandler(errorApp => {
					errorApp.Run(async context => {
						context.Response.StatusCode = 500;
						context.Response.ContentType = "text/html; charset=utf-8";
						var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
						Exception ex = exceptionHandlerPathFeature?.Error ?? new Exception("Erro desconhecido");
						await context.Response.WriteAsync(ExceptionPage.GetPage(ex, env));
					});
				});
			}
			app.UseHsts();
			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseSession();
			app.UseEndpoints(endpoints => {
				endpoints.MapControllerRoute(
					 name: "default",
					 pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
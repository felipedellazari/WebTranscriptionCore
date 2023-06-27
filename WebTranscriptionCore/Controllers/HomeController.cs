using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace WebTranscriptionCore.Controllers {
	public class HomeController : Controller {

		private readonly IConfiguration cfg;
		private readonly IWebHostEnvironment env;

		//colombo
		public static UserK userk;

		public HomeController(IConfiguration cfg, IWebHostEnvironment env) {
			this.cfg = cfg;
			this.env = env;
		}

		public IActionResult Index() {
#if DEBUG
			userk = UserK.GetByPassword(cfg, "adm", "senha");
			Configurations cfgs = new Configurations(cfg, userk.Id.Value);

			cfgs.LoadConfig();
			List<Claim> claims = new List<Claim> {
									 new Claim("userId", userk.Id.ToString()),
									 new Claim("userName", userk.Name),
									 new Claim("UserProfileIds", JsonConvert.SerializeObject(userk.ProfileIds.ToArray())),
									 new Claim("role", "1"),
									 new Claim("cfgs", JsonConvert.SerializeObject(cfgs)),
									 new Claim("WebRootPath", env.WebRootPath)
					 };
			Task t = HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "user", "role")));
			Task.WaitAll(t);
#else
			if (!User.Claims.Any(x => x.Type == "userId")) return RedirectToAction("Login", "User");
#endif
			return RedirectToAction("Index", "SessionTask");
		}

		public string VerifySession()
		{
				return userk.VerifySimultaneousConnection();
		}

		public string GetCurrentSession(){
			return userk.GetCurrentSession();
		}

	}
}
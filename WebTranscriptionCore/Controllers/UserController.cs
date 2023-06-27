using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebTranscriptionCore {
	public class UserController : Controller {

		private readonly IConfiguration cfg;
		private readonly IWebHostEnvironment env;

		public UserController(IConfiguration cfg, IWebHostEnvironment env) {
			this.cfg = cfg;
			this.env = env;
		}

		public ActionResult Login() {
			UserModel model = new UserModel();
			return View(model);
		}

		[HttpPost]
		public ActionResult Login(UserModel model) {
			if (!ModelState.IsValid) return View(model);
			LoginProvider lp = new LoginProvider(cfg);
			if (!lp.Authenticate(model.User, model.Password)) {
				ModelState.AddModelError("", lp.ErrorMsg);
				return View(model);
			}
			SignIn(lp.UserK);
			return RedirectToAction("Index", "SessionTask");
		}

		private void SignIn(UserK user) {
			Configurations cfgs = new Configurations(cfg, user.Id.Value);

			cfgs.LoadConfig();
			List<Claim> claims = new List<Claim> {
									 new Claim( "userId", user.Id.ToString()),
									 new Claim("userName", user.Name),
									 new Claim("UserProfileIds", JsonConvert.SerializeObject(user.ProfileIds)),
									 new Claim("role", "1"),
									 new Claim("cfgs", JsonConvert.SerializeObject(cfgs)),
									 new Claim("WebRootPath", env.WebRootPath)
					 };
			Task t = HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "user", "role")));
			t.Wait();
		}

		public ActionResult Register() {
			RegisterModel model = new RegisterModel();
			return View(model);
		}

		[HttpPost]
		public ActionResult Register(RegisterModel model) {
			if (!ModelState.IsValid) return View(model);
			LoginProvider lp = new LoginProvider(cfg);

			//original
			/*
			if (!lp.Authenticate(model.User, model.Password))
			{
				ModelState.AddModelError("", lp.ErrorMsg);
				return View(model);
			}
			*/

			//modificada
			//if (!lp.AuthenticateOnRegistry(model.User, model.Password)) {
			//	ModelState.AddModelError("", lp.ErrorMsg);
			//	return View(model);
			//}
			
			//Chamar API para ativação da licença.
			//Se retornar erro, retornar mensagem de erro para a página.
			//Se ativou licença com sucesso, salvar licença na tabela User, e logar na aplicação.
			
			//string result = lp.UserK.RegistryLicenseAsync(model.License);

			//if (!result.Equals("Sucesso"))
			//{
			//	ModelState.AddModelError("", "Erro ao registrar-se: " + result);
			//	return View(model);
			//}
			

			SignIn(lp.UserK);
			return RedirectToAction("Index", "SessionTask");
		}

		public ActionResult LogOff() {
			Task t = HttpContext.SignOutAsync();
			t.Wait();
			return RedirectToAction("Login", "User");
		}
	}
}
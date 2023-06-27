using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebTranscriptionCore {
	public class BaseClass {

		protected readonly IConfiguration cfg;
		protected readonly IEnumerable<Claim> claims;

		public long uId => BLL.CurrentUserId(claims);

		public Configurations cfgs => BLL.Cfgs(claims);

		public string WebRootPath => claims.First(x => x.Type == "WebRootPath").Value;

		public BaseClass() { }

		public BaseClass(IConfiguration cfg) => this.cfg = cfg;

		public BaseClass(IConfiguration cfg, IEnumerable<Claim> claims) {
			this.cfg = cfg;
			this.claims = claims;
		}

	}
}

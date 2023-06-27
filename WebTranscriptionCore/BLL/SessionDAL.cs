using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace WebTranscriptionCore {

	public class SessionDAL : BaseClass {

		public long Id { get; set; }
		public object Guid { get; set; }
		public string Description { get; set; }
		public DateTime ExpectedDate { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime FinishDate { get; set; }
		public string SessionTypeName { get; set; }
		public string PlaceName { get; set; }
		public string Number { get; set; }
		public int Status { get; set; }
		public long Duration { get; set; }

		public SessionDAL() { }

		public SessionDAL(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) { }
	}
}
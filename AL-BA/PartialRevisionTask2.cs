using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using WebTranscriptionCore;

namespace Kenta.DRSPlenario {
	class PartialRevisionTask2 : PartialRevisionTask {

		public PartialRevisionTask2(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims) {
		}

		public PartialRevisionTask2(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id) {
		}

		public PartialRevisionTask2(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId) {
		}

		public PartialRevisionTask2(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark) {
		}
	}
}
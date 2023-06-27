using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using WebTranscriptionCore;

namespace Kenta.DRSPlenario
{
	class PartialRevisionTask_ALSP : PartialRevisionTask
	{
		public PartialRevisionTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims) : base(cfg, claims)
		{
		}

		public PartialRevisionTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims, long id) : base(cfg, claims, id)
		{
		}

		public PartialRevisionTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId) : base(cfg, claims, sessionId, stepId)
		{
		}

		public PartialRevisionTask_ALSP(IConfiguration cfg, IEnumerable<Claim> claims, long sessionId, long stepId, long mark) : base(cfg, claims, sessionId, stepId, mark)
		{
		}
	}
}

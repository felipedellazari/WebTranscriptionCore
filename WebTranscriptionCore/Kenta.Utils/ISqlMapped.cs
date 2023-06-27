using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kenta.Utils.Data {
	public interface ISqlMapped {
		Object GetValueForDbParameter(DatabaseType type);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kenta.Utils.Data {
	public enum DatabaseType {
		Unknown,
		MSSQL,
		SqlCompact,
		Oracle,
		Firebird,
		MySQL,
		PostgreSQL,
		DB2,
		Informix
	}
}

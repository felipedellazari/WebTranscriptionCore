using System.Collections.Generic;
using System.Data;

namespace WebTranscriptionCore {
	public interface IConnectionProvider {
		string ConnString { get; set; }
		IEnumerable<T> Query<T>(string sql, object param);
		IEnumerable<dynamic> Query(string sql, object param);
		T QueryFirst<T>(string sql, object param);
		dynamic QueryFirst(string sql, object param);
		T ExecScalar<T>(string sql, object args);
		int Execute(string sql, object args);
		long Nextid(string tableName);
		int InsertSQL(string tableName, object args);
		int UpdateSQL(string tableName, object args, string whereField);
	}
}

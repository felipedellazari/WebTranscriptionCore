using System;
using System.Collections.Generic;

namespace WebTranscriptionCore {
	public class ConnectionManager {

		public string Provider { get; set; }

		public IConnectionProvider Connection { get; set; }

		public ConnectionManager(string provider, string cnnStr) {
			if (provider == "Oracle") Connection = new OracleConnectionProvider(cnnStr);
			else if (provider == "SqlServer") Connection = new SqlServerConnectionProvider(cnnStr);
			else if (provider == "PostgreSQL") Connection = new PostgreSQLConnectionProvider(cnnStr);
			else throw new Exception($"Provider { provider } não implementado.");
		}

		public IEnumerable<T> Query<T>(string sql, object args) => Connection.Query<T>(sql, args);

		public IEnumerable<dynamic> Query(string sql, object args) => Connection.Query(sql, args);

		public T QueryFirst<T>(string sql, object args) => Connection.QueryFirst<T>(sql, args);

		public dynamic QueryFirst(string sql, object args) => Connection.QueryFirst(sql, args);

		public T ExecScalar<T>(string sql, object args) => Connection.ExecScalar<T>(sql, args);

		public int Execute(string sql, object args) => Connection.Execute(sql, args);
		public long NextId(string tableName) => Connection.Nextid(tableName);

		public int InsertSQL<T>(object args) => InsertSQL(typeof(T).Name, args);

		public int InsertSQL(string tableName, object args) => Connection.InsertSQL(tableName, args);

		public int UpdateSQL<T>(object args, string whereField) => UpdateSQL(typeof(T).Name, args, whereField);

		public int UpdateSQL(string tableName, object args, string whereField) => Connection.UpdateSQL(tableName, args, whereField);

		public string InsertJunction(string foreignTable, string junctionTable, long localId, IEnumerable<long> foreignIds) => "INSERT INTO " + junctionTable
				+ " SELECT " + localId + ", p.Id "
				+ " FROM " + foreignTable + " p "
				+ " WHERE p.Id IN (" + string.Join(", ", foreignIds) + ")";

	}
}
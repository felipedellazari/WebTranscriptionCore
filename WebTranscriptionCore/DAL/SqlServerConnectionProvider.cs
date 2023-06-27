using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace WebTranscriptionCore {
	public class SqlServerConnectionProvider : IConnectionProvider {

		public string ConnString { get; set; }

		public SqlServerConnectionProvider(string connStr) => ConnString = connStr;

		private void FormatArgs(ref string sql, object args) {
			if (args == null) return;
			if (args is Dictionary<string, object> dic) {
				foreach (var item in dic)
					sql = sql.Replace(":" + item.Key, "@" + item.Key);
			} else {
				foreach (PropertyInfo item in args.GetType().GetProperties())
					sql = sql.Replace(":" + item.Name, "@" + item.Name);
			}
		}

		public IEnumerable<T> Query<T>(string sql, object args) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.Query<T>(sql, args);
		}

		public IEnumerable<dynamic> Query(string sql, object args) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.Query(sql, args);
		}

		public T QueryFirst<T>(string sql, object args) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.QueryFirstOrDefault<T>(sql, args);
		}

		public dynamic QueryFirst(string sql, object args) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.QueryFirstOrDefault(sql, args);
		}

		public T ExecScalar<T>(string sql, object args = null) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.ExecuteScalar<T>(sql, args);
		}

		public int Execute(string sql, object args) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.Execute(sql, args);
		}

		public T QuerySingle<T>(string sql, object args) {
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			return connection.QuerySingle<T>(sql, args);
		}

		public long Nextid(string tableName) {
			string sql = "SELECT COUNT(*) FROM SYSOBJECTS WHERE XTYPE = 'U' AND NAME = 'SQ_" + tableName + "'";
			long existsSQ_Table = ExecScalar<long>(sql, null);
			if (existsSQ_Table > 0)
				return QuerySingle<long>(@"INSERT INTO [SQ_" + tableName + @"] DEFAULT VALUES; SELECT SCOPE_IDENTITY();", null);
			else
				return QueryFirst<long>(@"SELECT NEXT VALUE FOR [SQ_" + tableName + "]", null);
		}

		public int InsertSQL(string tableName, object args) {
			List<SqlParameter> pars = new List<SqlParameter>();
			string sql = "INSERT INTO " + tableName + " (";
			foreach (PropertyInfo item in args.GetType().GetProperties())
				sql += "\"" + item.Name.ToUpper() + "\"" + ",";
			sql = sql.Substring(0, sql.Length - 1);
			sql += ") VALUES (";
			foreach (PropertyInfo item in args.GetType().GetProperties()) {
				object? val = item.GetValue(args);
				if (val == null)
					sql += "NULL";
				else if (item.PropertyType == typeof(string)) {
					sql += "@" + item.Name.ToUpper();
					SqlParameter par = new SqlParameter(item.Name.ToUpper(), SqlDbType.NVarChar, -1) {
						Value = val.ToString()
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Clob)) {
					sql += "@" + item.Name.ToUpper();
					SqlParameter par = new SqlParameter(item.Name.ToUpper(), SqlDbType.NVarChar, -1) {
						Value = (val as Clob).Text
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Blob)) {
					sql += "@" + item.Name.ToUpper();
					SqlParameter par = new SqlParameter(item.Name.ToUpper(), SqlDbType.VarBinary, -1) {
						Value = (val as Blob).Value
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(DateTime))
					sql += "CONVERT(DATETIME,'" + Convert.ToDateTime(val).ToString("yyyy-MM-dd HH:mm:ss") + "',120)";
				else
					sql += val;
				sql += ",";
			}
			sql = sql.Substring(0, sql.Length - 1);
			sql += ")";
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			SqlCommand cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.Parameters.AddRange(pars.ToArray());
			connection.Open();
			return cmd.ExecuteNonQuery();
		}

		public int UpdateSQL(string tableName, object args, string whereField) {
			List<SqlParameter> pars = new List<SqlParameter>();
			string whereValue = "";
			string sql = "UPDATE " + tableName + " SET ";
			foreach (PropertyInfo item in args.GetType().GetProperties()) {
				object? val = item.GetValue(args);
				if (item.Name == whereField) {
					whereValue = val.ToString();
					continue;
				}
				sql += "\"" + item.Name.ToUpper() + "\"" + " = ";
				if (val == null)
					sql += "NULL";
				else if (item.PropertyType == typeof(string)) {
					sql += "@" + item.Name.ToUpper();
					SqlParameter par = new SqlParameter(item.Name.ToUpper(), SqlDbType.NVarChar, -1) {
						Value = val.ToString()
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Clob)) {
					sql += "@" + item.Name.ToUpper();
					SqlParameter par = new SqlParameter(item.Name.ToUpper(), SqlDbType.NVarChar, -1) {
						Value = (val as Clob).Text
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Blob)) {
					sql += "@" + item.Name.ToUpper();
					SqlParameter par = new SqlParameter(item.Name.ToUpper(), SqlDbType.VarBinary, -1) {
						Value = (val as Blob).Value
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(DateTime))
					sql += "CONVERT(DATETIME,'" + Convert.ToDateTime(val).ToString("yyyy-MM-dd HH:mm:ss") + "',120)";
				else
					sql += item.GetValue(args);
				sql += ", ";
			}
			sql = sql.Substring(0, sql.Length - 2);
			sql += " WHERE " + whereField + " = " + whereValue;
			FormatArgs(ref sql, args);
			using SqlConnection connection = new SqlConnection(ConnString);
			SqlCommand cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.Parameters.AddRange(pars.ToArray());
			connection.Open();
			return cmd.ExecuteNonQuery();
		}
	}
}
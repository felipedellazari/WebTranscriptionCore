using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WebTranscriptionCore {
	public class PostgreSQLConnectionProvider : IConnectionProvider {
		public string ConnString { get; set; }

		protected static Regex rgQuotes = new Regex(@"(?<=[^a-z0-9_])""[a-z0-9_]+""(?=(?:[^']|'([^']|'')*')*$)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

		public PostgreSQLConnectionProvider(string connStr) {
			ConnString = connStr;
		}

		private void FormatArgs(ref string sql, object args) {

			String s = rgQuotes.Replace(sql, x => x.Value.ToLower());
			s.GetHashCode();
			sql = s;

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
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.Query<T>(sql, args);
		}

		public IEnumerable<dynamic> Query(string sql, object args) {
			FormatArgs(ref sql, args);
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.Query(sql, args);
		}

		public T QueryFirst<T>(string sql, object args) {
			FormatArgs(ref sql, args);
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.QueryFirstOrDefault<T>(sql, args);
		}

		public dynamic QueryFirst(string sql, object args) {
			FormatArgs(ref sql, args);
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.QueryFirstOrDefault(sql, args);
		}

		public T ExecScalar<T>(string sql, object args) {
			FormatArgs(ref sql, args);
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.ExecuteScalar<T>(sql, args);
		}

		public int Execute(string sql, object args) {
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.Execute(sql, args);
		}

		public long Nextid(string tableName) {
			string sql = @"SELECT NEXTVAL ('SQ_" + tableName + "')";

			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			return connection.QueryFirst<long>(sql);
		}

		public int InsertSQL(string tableName, object args) {
			List<NpgsqlParameter> pars = new List<NpgsqlParameter>();
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
					sql += ":" + item.Name.ToUpper();
					NpgsqlParameter par = new NpgsqlParameter(item.Name.ToUpper(), NpgsqlTypes.NpgsqlDbType.Varchar) {
						Value = val.ToString()
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Clob)) {
					sql += ":" + item.Name.ToUpper();
					NpgsqlParameter par = new NpgsqlParameter(item.Name.ToUpper(), NpgsqlTypes.NpgsqlDbType.Varchar) {
						Value = (val as Clob).Text
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(DateTime))
					sql += "TO_DATE('" + Convert.ToDateTime(val).ToString("yyyy/MM/dd HH:mm:ss") + "', 'YYYY/MM/DD HH24:MI:SS')";
				else
					sql += val;
				sql += ",";
			}
			sql = sql.Substring(0, sql.Length - 1);
			sql += ")";
			FormatArgs(ref sql, args);
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			NpgsqlCommand cmd = connection.CreateCommand();
			cmd.Connection = connection;
			cmd.CommandText = sql;
			cmd.Parameters.AddRange(pars.ToArray());
			connection.Open();
			return cmd.ExecuteNonQuery();
		}

		public int UpdateSQL(string tableName, object args, string whereField) {
			List<NpgsqlParameter> pars = new List<NpgsqlParameter>();
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
					sql += ":" + item.Name.ToUpper();
					NpgsqlParameter par = new NpgsqlParameter(item.Name.ToUpper(), NpgsqlTypes.NpgsqlDbType.Varchar) {
						Value = val.ToString()
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Clob)) {
					sql += ":" + item.Name.ToUpper();
					NpgsqlParameter par = new NpgsqlParameter(item.Name.ToUpper(), NpgsqlTypes.NpgsqlDbType.Varchar) {
						Value = (val as Clob).Text
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(DateTime))
					sql += "TO_DATE('" + Convert.ToDateTime(val).ToString("yyyy/MM/dd HH:mm:ss") + "', 'YYYY/MM/DD HH24:MI:SS')";
				else
					sql += item.GetValue(args);
				sql += ", ";
			}
			sql = sql.Substring(0, sql.Length - 2);
			sql += " WHERE " + whereField + " = " + whereValue;
			FormatArgs(ref sql, args);
			using NpgsqlConnection connection = new NpgsqlConnection(ConnString);
			NpgsqlCommand cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.Parameters.AddRange(pars.ToArray());
			connection.Open();
			return cmd.ExecuteNonQuery();
		}

	}
}

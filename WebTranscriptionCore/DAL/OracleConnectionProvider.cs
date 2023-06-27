using Dapper;
using Kenta.Utils;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebTranscriptionCore {
	public class OracleConnectionProvider : IConnectionProvider {

		public string ConnString { get; set; }

		public OracleConnectionProvider(string connStr) {
			ConnString = connStr;
		}

		public IEnumerable<T> Query<T>(string sql, object args) {
			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.Query<T>(sql, args);
		}

		public IEnumerable<dynamic> Query(string sql, object args) {
			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.Query(sql, args);
		}

		public T QueryFirst<T>(string sql, object args) {
			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.QueryFirstOrDefault<T>(sql, args);
		}

		public dynamic QueryFirst(string sql, object args) {
			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.QueryFirstOrDefault(sql, args);
		}

		public T ExecScalar<T>(string sql, object args) {
			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.ExecuteScalar<T>(sql, args);
		}

		public int Execute(string sql, object args) {
			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.Execute(sql, args);
		}

		public long Nextid(string tableName) {
			string sql = $"select sq_{tableName}.nextval from dual";

			using OracleConnection connection = new OracleConnection(ConnString);
			return connection.QueryFirst<long>(sql);
		}

		public int InsertSQL(string tableName, object args) {
			List<OracleParameter> pars = new List<OracleParameter>();
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
					OracleParameter par = new OracleParameter(item.Name.ToUpper(), OracleDbType.Varchar2) {
						Value = val.ToString()
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Clob)) {
					sql += ":" + item.Name.ToUpper();
					OracleParameter par = new OracleParameter(item.Name.ToUpper(), OracleDbType.Clob) {
						Value = (val as Clob).Text
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Blob)) {
					sql += ":" + item.Name.ToUpper();
					OracleParameter par = new OracleParameter(item.Name.ToUpper(), OracleDbType.Blob) {
						Value = (val as Blob).Value
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
			using OracleConnection connection = new OracleConnection(ConnString);
			OracleCommand cmd = connection.CreateCommand();
			cmd.Connection = connection;
			cmd.CommandText = sql;
			cmd.Parameters.AddRange(pars.ToArray());
			connection.Open();
			return cmd.ExecuteNonQuery();
		}

		public int UpdateSQL(string tableName, object args, string whereField) {
			List<OracleParameter> pars = new List<OracleParameter>();
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
					OracleParameter par = new OracleParameter(item.Name.ToUpper(), OracleDbType.Varchar2) {
						Value = val.ToString()
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Clob)) {
					sql += ":" + item.Name.ToUpper();
					OracleParameter par = new OracleParameter(item.Name.ToUpper(), OracleDbType.Clob) {
						Value = (val as Clob).Text
					};
					pars.Add(par);
				} else if (item.PropertyType == typeof(Blob)) {
					sql += ":" + item.Name.ToUpper();
					OracleParameter par = new OracleParameter(item.Name.ToUpper(), OracleDbType.Blob) {
						Value = (val as Blob).Value
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
			using OracleConnection connection = new OracleConnection(ConnString);
			OracleCommand cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.Parameters.AddRange(pars.ToArray());
			connection.Open();
			return cmd.ExecuteNonQuery();
		}
	}
}
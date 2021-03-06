﻿using System;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace Reports
{
	public class Database
	{
		private IDbConnection _db;

		public bool MySQL { get { return _db.GetSqlType() == SqlType.Mysql; } }

		public Database(IDbConnection db)
		{
			_db = db;

			//Define a table creator that will be responsible for ensuring the database table exists
			var sqlCreator = new SqlTableCreator(_db,
				_db.GetSqlType() == SqlType.Sqlite
					? (IQueryBuilder) new SqliteQueryCreator()
					: new MysqlQueryCreator());

			//Define the table
			var table = new SqlTable("Reports",
				new SqlColumn("ReportID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("ReportedID", MySqlDbType.Int32),
				new SqlColumn("Message", MySqlDbType.Text),
				new SqlColumn("Position", MySqlDbType.Text),
				new SqlColumn("State", MySqlDbType.Int32));

			//Create the table if it doesn't exist, update the structure if it exists but is not the same as
			//the table defined above, or do nothing if the table exists and is correctly structured
			sqlCreator.EnsureTableStructure(table);
		}

		/// <summary>
		/// Creates and returns an instance of <see cref="Database"/>
		/// </summary>
		/// <param name="name">File name (without .sqlite) if using SQLite, database name if using MySQL</param>
		/// <returns>Instance of <see cref="Database"/></returns>
		public static Database InitDb(string name)
		{
			IDbConnection db;
			if (TShock.Config.StorageType.ToLower() == "sqlite")
			{
				//Creates the database connection
				db = new SqliteConnection(string.Format("uri=file://{0},Version=3",
						  Path.Combine(TShock.SavePath, name + ".sqlite")));
			}
			else if (TShock.Config.StorageType.ToLower() == "mysql")
			{
				try
				{
					var host = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
							host[0],
							host.Length == 1 ? "3306" : host[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword
							)
					};
				}
				catch (MySqlException x)
				{
					TShock.Log.Error(x.ToString());
					throw new Exception("MySQL not setup correctly.");
				}
			}
			else
				throw new Exception("Invalid storage type.");
			
			return new Database(db);
		}

		public QueryResult QueryReader(string query, params object[] args)
		{
			return _db.QueryReader(query, args);
		}

		public int Query(string query, params object[] args)
		{
			return _db.Query(query, args);
		}

		public bool DeleteValue(string column, object value)
		{
			var query = string.Format("DELETE FROM Reports WHERE {0} = @0", column);
			return _db.Query(query, value) > 0;
		}

		public bool SetValue(string column, object value, string whereColumn, object where)
		{
			var query = string.Format("UPDATE Reports SET {0} = @0 WHERE {1} = @1", column, whereColumn);
			return _db.Query(query, value, where) > 0;
		}
	}
}
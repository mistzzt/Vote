using System;
using System.Data;
using MySql.Data.MySqlClient;
using TShockAPI.DB;

namespace Vote {
	internal class VoteManager {
		private IDbConnection _database;

		public VoteManager(IDbConnection db) {
			_database = db;

			var table = new SqlTable("Votes",
									 new SqlColumn("Date", MySqlDbType.Text) { Primary = true },
									 new SqlColumn("Sponsor", MySqlDbType.Text),
									 new SqlColumn("Target", MySqlDbType.Text),
									 new SqlColumn("Type", MySqlDbType.String, 7),
									 new SqlColumn("Reason", MySqlDbType.Text),
									 new SqlColumn("Succeed", MySqlDbType.Int32),
									 new SqlColumn("Proponents", MySqlDbType.Text),
									 new SqlColumn("Opponents", MySqlDbType.Text)
				);
			var creator = new SqlTableCreator(db,
											  db.GetSqlType() == SqlType.Sqlite
												  ? (IQueryBuilder)new SqliteQueryCreator()
												  : new MysqlQueryCreator());
			try {
				creator.EnsureTableStructure(table);
			} catch(DllNotFoundException) {
				Console.WriteLine(@"Possible problem with your database -- is Sqlite3.dll present?");
				throw new Exception("Could not find a database library (probably Sqlite3.dll)");
			}
		}

		public void AddVote(Vote vote) {

		}
	}
}

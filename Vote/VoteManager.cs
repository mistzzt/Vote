using System;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;

namespace Vote {
	internal class VoteManager {
		private IDbConnection _database;

		public VoteManager(IDbConnection db) {
			_database = db;

			var table = new SqlTable("Votes",
									new SqlColumn("Type", MySqlDbType.String, 7) { Primary = true },
									new SqlColumn("Date", MySqlDbType.Text),
									new SqlColumn("Sponsor", MySqlDbType.Text),
									new SqlColumn("Target", MySqlDbType.Text),
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
			const string query = "INSERT INTO `Votes` (`Type`, `Date`, `Sponsor`, `Target`, `Reason`, `Succeed`, `Proponents`, `Opponents`) VALUES (@0, @1, @2, @3, @4, @5, @6, @7);";
			var parameters = new object[] {
				vote.Type.ToString(),
				vote.Time.ToString("s"),
				vote.Sponsor,
				vote.Target,
				vote.Reason,
				vote.Succeed ? 1 : 0,
				JsonConvert.SerializeObject(vote.Proponents, Formatting.Indented),
				JsonConvert.SerializeObject(vote.Opponents, Formatting.None)
			};
			try {
				if(_database.Query(query, parameters) == 0)
					throw new Exception("No affected rows");
			} catch(Exception ex) {
				TShock.Log.Error(ex.ToString());
				Debugger.Break();
			}
		}
	}
}

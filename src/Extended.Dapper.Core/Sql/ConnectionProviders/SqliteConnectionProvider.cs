using System;
using System.Data;
using System.Data.SQLite;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.ConnectionProviders
{
    public class SqliteConnectionProvider : ConnectionProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataSource">Location of the .db file</param>
        public SqliteConnectionProvider(string dataSource)
        {
            this.ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = dataSource, BinaryGUID = true }.ToString();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbSettings">Use the "database" field to locate the .db file</param>
        public SqliteConnectionProvider(DatabaseSettings dbSettings)
        {
            this.ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = dbSettings.Database, BinaryGUID = true }.ToString();
        }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
            => new SQLiteConnection(this.ConnectionString, true);

        /// <summary>
        /// - NOT IMPLEMENTED FOR SQLite -
        /// </summary>
        protected override string BuildConnectionString(DatabaseSettings databaseSettings)
            => throw new NotImplementedException();
    }
}
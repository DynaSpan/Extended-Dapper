using System;
using System.Data;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.ConnectionProviders
{
    public abstract class ConnectionProvider : IConnectionProvider
    {
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Empty constructor
        /// </summary>
        protected ConnectionProvider()
        {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        protected ConnectionProvider(DatabaseSettings databaseSettings)
        {
            this.ConnectionString = this.BuildConnectionString(databaseSettings);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        protected ConnectionProvider(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

         /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        protected abstract string BuildConnectionString(DatabaseSettings databaseSettings);

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public abstract IDbConnection GetConnection();

        /// <summary>
        /// Gets the correct connection provider
        /// </summary>
        /// <param name="dbSettings"></param>
        public static IConnectionProvider GetConnectionProvider(DatabaseSettings dbSettings)
        {
            return dbSettings.DatabaseProvider switch
            {
                DatabaseProvider.MSSQL => new MsSqlConnectionProvider(dbSettings),
                DatabaseProvider.MySQL => new MySqlConnectionProvider(dbSettings),
                DatabaseProvider.SQLite => new SqliteConnectionProvider(dbSettings),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Gets the correct connection provider
        /// </summary>
        /// <param name="databaseProvider"></param>
        /// <param name="connectionString"></param>
        public static IConnectionProvider GetConnectionProvider(DatabaseProvider databaseProvider, string connectionString)
        {
            return databaseProvider switch
            {
                DatabaseProvider.MSSQL => new MsSqlConnectionProvider(connectionString),
                DatabaseProvider.MySQL => new MySqlConnectionProvider(connectionString),
                DatabaseProvider.SQLite => new SqliteConnectionProvider(connectionString),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
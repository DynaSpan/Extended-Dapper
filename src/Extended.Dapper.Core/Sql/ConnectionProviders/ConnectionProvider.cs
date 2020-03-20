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
        public ConnectionProvider()
        {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        public ConnectionProvider(DatabaseSettings databaseSettings)
        {
            this.ConnectionString = this.BuildConnectionString(databaseSettings);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public ConnectionProvider(string connectionString)
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
            switch (dbSettings.DatabaseProvider)
            {
                case DatabaseProvider.MSSQL:
                    return new MsSqlConnectionProvider(dbSettings);
                case DatabaseProvider.MySQL:
                    return new MySqlConnectionProvider(dbSettings);
                case DatabaseProvider.SQLite:
                    return new SqliteConnectionProvider(dbSettings);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the correct connection provider
        /// </summary>
        /// <param name="databaseProvider"></param>
        /// <param name="connectionString"></param>
        public static IConnectionProvider GetConnectionProvider(DatabaseProvider databaseProvider, string connectionString)
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.MSSQL:
                    return new MsSqlConnectionProvider(connectionString);
                case DatabaseProvider.MySQL:
                    return new MySqlConnectionProvider(connectionString);
                case DatabaseProvider.SQLite:
                    return new SqliteConnectionProvider(connectionString);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Dapper;
using Extended.Dapper.Core.Sql.ConnectionProviders;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Database 
{
    public class DatabaseFactory : IDatabaseFactory
    {
        public DatabaseProvider DatabaseProvider { get; }
        public ISqlQueryProvider SqlProvider { get; set; }
        public IConnectionProvider SqlConnectionProvider { get; set; }

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="databaseSettings"></param>
        public DatabaseFactory(DatabaseSettings databaseSettings)
        {
            this.DatabaseProvider       = databaseSettings.DatabaseProvider;
            this.SqlProvider            = SqlQueryProviderHelper.GetProvider(databaseSettings.DatabaseProvider);
            this.SqlConnectionProvider  = ConnectionProvider.GetConnectionProvider(databaseSettings);
        }

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="connectionString">Your own connection string according to the databaseProvider</param>
        /// <param name="databaseProvider"></param>
        public DatabaseFactory(string connectionString, DatabaseProvider databaseProvider)
        {
            this.DatabaseProvider       = databaseProvider;
            this.SqlProvider            = SqlQueryProviderHelper.GetProvider(databaseProvider);   
            this.SqlConnectionProvider  = ConnectionProvider.GetConnectionProvider(databaseProvider, connectionString);   
        }

        /// <summary>
        /// Creates a new database connection
        /// </summary>
        public IDbConnection GetDatabaseConnection()
            => this.SqlConnectionProvider.GetConnection();
    }
}
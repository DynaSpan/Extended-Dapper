using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using Extended.Dapper.Core.Sql.QueryProviders;
using MySql.Data.MySqlClient;

namespace Extended.Dapper.Core.Database 
{
    public class DatabaseFactory : IDatabaseFactory
    {
        public DatabaseProvider DatabaseProvider { get; }
        public ISqlQueryProvider SqlProvider { get; set; }

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="databaseSettings"></param>
        public DatabaseFactory(DatabaseSettings databaseSettings)
        {
            SqlQueryProviderHelper.SetProvider(databaseSettings.DatabaseProvider, databaseSettings);

            this.DatabaseProvider = databaseSettings.DatabaseProvider;
            this.SqlProvider      = SqlQueryProviderHelper.GetProvider();
        }

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="connectionString">Your own connection string according to the databaseProvider</param>
        /// <param name="databaseProvider"></param>
        public DatabaseFactory(string connectionString, DatabaseProvider databaseProvider)
        {
            SqlQueryProviderHelper.SetProvider(databaseProvider, connectionString);

            this.DatabaseProvider = databaseProvider;
            this.SqlProvider      = SqlQueryProviderHelper.GetProvider();      
        }

        /// <summary>
        /// Creates a new database connection
        /// </summary>
        public IDbConnection GetDatabaseConnection()
        {
            return this.SqlProvider.GetConnection();
        }
    }
}
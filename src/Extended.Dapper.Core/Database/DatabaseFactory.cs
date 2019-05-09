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
        private readonly string connectionString;
        public DatabaseProvider DatabaseProvider { get; }

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="databaseSettings"></param>
        public DatabaseFactory(DatabaseSettings databaseSettings)
        {
            SqlQueryProviderHelper.SetProvider(databaseSettings.DatabaseProvider);

            this.connectionString = this.ConstructConnectionString(databaseSettings);
            this.DatabaseProvider = databaseSettings.DatabaseProvider;
        }

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="connectionString">Your own connection string according to the databaseProvider</param>
        /// <param name="databaseProvider"></param>
        public DatabaseFactory(string connectionString, DatabaseProvider databaseProvider)
        {
            SqlQueryProviderHelper.SetProvider(databaseProvider);

            this.connectionString = connectionString;
            this.DatabaseProvider = databaseProvider;            
        }

        /// <summary>
        /// Creates a new database connection
        /// </summary>
        /// <returns></returns>
        public IDbConnection GetDatabaseConnection()
        {
            switch (this.DatabaseProvider)
            {
                case DatabaseProvider.MSSQL:
                    return new SqlConnection(this.connectionString);
                case DatabaseProvider.MySQL:
                    return new MySqlConnection(this.connectionString);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Constructs the connection string based on the
        /// DatabaseSettings
        /// </summary>
        /// <param name="databaseSettings"></param>
        /// <returns></returns>
        private string ConstructConnectionString(DatabaseSettings databaseSettings)
        {
            var provider = SqlQueryProviderHelper.GetProvider();

            if (provider == null)
                throw new NotImplementedException();

            return provider.BuildConnectionString(databaseSettings);
        }
    }
}
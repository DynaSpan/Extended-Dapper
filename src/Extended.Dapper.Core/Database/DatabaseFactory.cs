using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using Extended.Dapper.Core.Sql.Providers;
using MySql.Data.MySqlClient;

namespace Extended.Dapper.Core.Database 
{
    public class DatabaseFactory : IDatabaseFactory
    {
        private readonly string connectionString;
        private readonly DatabaseProvider databaseProvider;

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="databaseSettings"></param>
        public DatabaseFactory(DatabaseSettings databaseSettings)
        {
            this.connectionString = this.ConstructConnectionString(databaseSettings);
            this.databaseProvider = databaseSettings.DatabaseProvider;
        }

        public DatabaseFactory(string connectionString, DatabaseProvider databaseProvider)
        {
            this.connectionString = connectionString;
            this.databaseProvider = databaseProvider;
        }

        /// <inheritdoc />
        public IDbConnection GetDatabaseConnection()
        {
            switch (this.databaseProvider)
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
            var provider = SqlProviderHelper.GetProvider(databaseSettings.DatabaseProvider);

            if (provider == null)
                throw new NotImplementedException();

            return provider.BuildConnectionString(databaseSettings);
        }
    }
}
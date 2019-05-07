using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;

namespace Extended.Dapper.Core.Database 
{
    public class DatabaseFactory : IDatabaseFactory
    {
        private readonly string connectionString;

        /// <summary>
        /// Constructor for the factory
        /// </summary>
        /// <param name="dbSettings"></param>
        public DatabaseFactory(DatabaseSettings dbSettings)
        {
            this.connectionString = this.ConstructConnectionString(dbSettings);
        }

        /// <inheritdoc />
        public IDbConnection GetDatabaseConnection()
        {
            return new SqlConnection(this.connectionString);
        }

        /// <summary>
        /// Constructs the connection string based on the
        /// DatabaseSettings
        /// </summary>
        /// <param name="dbSettings"></param>
        /// <returns></returns>
        private string ConstructConnectionString(DatabaseSettings dbSettings)
        {
            StringBuilder connStringBuilder = new StringBuilder();

            if (dbSettings.Port != null)
                connStringBuilder.AppendFormat("Server={0},{1};", dbSettings.Host, dbSettings.Port);
            else
                connStringBuilder.AppendFormat("Server={0};", dbSettings.Host);
            
            connStringBuilder.AppendFormat("Database={0};", dbSettings.Database);
            connStringBuilder.AppendFormat("User Id={0};", dbSettings.User);
            connStringBuilder.AppendFormat("Password={0};", dbSettings.Password);

            if (dbSettings.TrustedConnection)
                connStringBuilder.Append("Trusted_Connection=true;");

            return connStringBuilder.ToString();
        }
    }
}
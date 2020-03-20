using System.Data;
using System.Text;
using Extended.Dapper.Core.Database;
using MySql.Data.MySqlClient;

namespace Extended.Dapper.Core.Sql.ConnectionProviders
{
    public class MySqlConnectionProvider : ConnectionProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        /// <returns></returns>
        public MySqlConnectionProvider(DatabaseSettings databaseSettings) : base(databaseSettings)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public MySqlConnectionProvider(string connectionString) : base(connectionString)
        { }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
            => new MySqlConnection(this.ConnectionString);

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        protected override string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            StringBuilder connStringBuilder = new StringBuilder();

            connStringBuilder.AppendFormat("Server={0};", databaseSettings.Host);

            if (databaseSettings.Port != null)
                connStringBuilder.AppendFormat("Port={0};", databaseSettings.Port);

            if (databaseSettings.Database != null)
                connStringBuilder.AppendFormat("Database={0};", databaseSettings.Database);

            connStringBuilder.AppendFormat("Uid={0};", databaseSettings.User);
            connStringBuilder.AppendFormat("Pwd={0};", databaseSettings.Password);

            return connStringBuilder.ToString();
        }
    }
}
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.ConnectionProviders
{
    public class MsSqlConnectionProvider : ConnectionProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        public MsSqlConnectionProvider(DatabaseSettings databaseSettings) : base(databaseSettings)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public MsSqlConnectionProvider(string connectionString) : base(connectionString)
        { }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
            => new SqlConnection(this.ConnectionString);

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        protected override string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            StringBuilder connStringBuilder = new StringBuilder();

            if (databaseSettings.Port != null)
                connStringBuilder.AppendFormat("Server={0},{1};", databaseSettings.Host, databaseSettings.Port);
            else
                connStringBuilder.AppendFormat("Server={0};", databaseSettings.Host);
            
            if (databaseSettings.Database != null)
                connStringBuilder.AppendFormat("Database={0};", databaseSettings.Database);
                
            connStringBuilder.AppendFormat("User Id={0};", databaseSettings.User);
            connStringBuilder.AppendFormat("Password={0};", databaseSettings.Password);

            if (databaseSettings.TrustedConnection != null && databaseSettings.TrustedConnection == true) // doesnt work without implicit true
                connStringBuilder.Append("Trusted_Connection=true;");

            return connStringBuilder.ToString();
        }
    }
}
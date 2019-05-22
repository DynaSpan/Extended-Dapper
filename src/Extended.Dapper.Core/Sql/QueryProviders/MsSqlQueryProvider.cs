using System.Data;
using System.Data.SqlClient;
using System.Text;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class MsSqlQueryProvider : SqlQueryProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        public MsSqlQueryProvider(DatabaseSettings databaseSettings) : base(databaseSettings)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public MsSqlQueryProvider(string connectionString) : base(connectionString)
        {

        }

        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        public override string EscapeTable(string tableName)
        {
            return "[" + tableName + "]";
        }

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public override string EscapeColumn(string columnName)
        {
            return "[" + columnName + "]";
        }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
        {
            return new SqlConnection(this.ConnectionString);
        }

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        public override string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            StringBuilder connStringBuilder = new StringBuilder();

            if (databaseSettings.Port != null)
                connStringBuilder.AppendFormat("Server={0},{1};", databaseSettings.Host, databaseSettings.Port);
            else
                connStringBuilder.AppendFormat("Server={0};", databaseSettings.Host);
            
            connStringBuilder.AppendFormat("Database={0};", databaseSettings.Database);
            connStringBuilder.AppendFormat("User Id={0};", databaseSettings.User);
            connStringBuilder.AppendFormat("Password={0};", databaseSettings.Password);

            if (databaseSettings.TrustedConnection != null && databaseSettings.TrustedConnection == true) // doesnt work without implicit true
                connStringBuilder.Append("Trusted_Connection=true;");

            return connStringBuilder.ToString();
        }
    }
}
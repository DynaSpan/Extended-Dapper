using System.Data;
using System.Linq.Expressions;
using System.Text;
using Extended.Dapper.Core.Database;
using MySql.Data.MySqlClient;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class MySqlQueryProvider : SqlQueryProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        /// <returns></returns>
        public MySqlQueryProvider(DatabaseSettings databaseSettings) : base(databaseSettings)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public MySqlQueryProvider(string connectionString) : base(connectionString)
        {

        }

        /// <summary>
        /// The char used for parameters
        /// </summary>
        public override string ParameterChar { get { return "@"; } }        

        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        public override string EscapeTable(string tableName)
        {
            return "`" + tableName + "`";
        }

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public override string EscapeColumn(string columnName)
        {
            return "`" + columnName + "`";
        }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
        {
            return new MySqlConnection(this.ConnectionString);
        }

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        public override string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            StringBuilder connStringBuilder = new StringBuilder();

            connStringBuilder.AppendFormat("Server={0};", databaseSettings.Host);

            if (databaseSettings.Port != null)
                connStringBuilder.AppendFormat("Port={0};", databaseSettings.Port);

            connStringBuilder.AppendFormat("Database={0};", databaseSettings.Database);
            connStringBuilder.AppendFormat("Uid={0};", databaseSettings.User);
            connStringBuilder.AppendFormat("Pwd={0};", databaseSettings.Password);

            return connStringBuilder.ToString();
        }
    }
}
using System.Text;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.Providers
{
    public class MySqlProvider : SqlProvider
    {
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
using System.Text;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.Providers
{
    public class MySqlProvider : ISqlProvider
    {
        /// <inheritdoc />
        public string EscapeTable(string tableName)
        {
            return "`" + tableName + "`";
        }

        /// <inheritdoc />
        public string EscapeColumn(string columnName)
        {
            return "`" + columnName + "`";
        }

        /// <inheritdoc />
        public string BuildConnectionString(DatabaseSettings databaseSettings)
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
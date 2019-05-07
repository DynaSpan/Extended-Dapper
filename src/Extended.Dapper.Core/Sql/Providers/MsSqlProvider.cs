using System.Text;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.Providers
{
    public class MsSqlProvider : ISqlProvider
    {
        /// <inheritdoc />
        public string EscapeTable(string tableName)
        {
            return "[" + tableName + "]";
        }

        /// <inheritdoc />
        public string EscapeColumn(string columnName)
        {
            return "[" + columnName + "]";
        }

        /// <inheritdoc />
        public string BuildConnectionString(DatabaseSettings databaseSettings)
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
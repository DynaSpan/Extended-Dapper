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
    }
}
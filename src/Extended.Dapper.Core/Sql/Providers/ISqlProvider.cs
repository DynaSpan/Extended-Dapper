namespace Extended.Dapper.Core.Sql.Providers
{
    public interface ISqlProvider
    {
        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string EscapeTable(string tableName);

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        string EscapeColumn(string columnName);
    }
}
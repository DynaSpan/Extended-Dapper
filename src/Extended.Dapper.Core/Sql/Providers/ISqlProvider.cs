using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;

namespace Extended.Dapper.Core.Sql.Providers
{
    public interface ISqlProvider
    {
        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        string EscapeTable(string tableName);

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        string EscapeColumn(string columnName);

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        string BuildConnectionString(DatabaseSettings databaseSettings);

        /// <summary>
        /// Generates the SQL select fields for a given entity
        /// </summary>
        /// <param name="entityMap"></param>
        /// <returns></returns>
        string GenerateSelectFields(EntityMap entityMap);
    }
}
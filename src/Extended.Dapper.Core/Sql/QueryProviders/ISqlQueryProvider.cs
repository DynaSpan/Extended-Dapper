using System;
using System.Linq.Expressions;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public interface ISqlQueryProvider
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

        /// <summary>
        /// Converts an ExpressionType to a SQL operator
        /// </summary>
        /// <param name="type"></param>
        /// <returns>SQL operator as string</returns>
        string GetSqlOperator(ExpressionType type);

        /// <summary>
        /// Gets a value in the correct SQL format
        /// (for LINQ string stuff such as StartsWith, Contains, etc.)
        /// </summary>
        /// <param name="methodName">Name of the LINQ method</param>
        /// <param name="value"></param>
        string GetSqlLikeValue(string methodName, object value);

        /// <summary>
        /// Gets the SQL selector for methodName.
        /// </summary>
        /// <param name="methodName">Name of the LINQ method</param>
        /// <param name="isNotUnary">Indicates if the selection should be 
        /// reversed (e.g. IN => NOT IN)</param>
        string GetMethodCallSqlOperator(string methodName, bool isNotUnary = false);

        /// <summary>
        /// Generates a where clause for a query based
        /// on the predicate
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <param name="predicate"></param>
        /// <param name="queryType"></param>
        /// <param name="entityMap"></param>
        void AppendWherePredicateQuery<T>(SelectSqlQuery sqlQuery, Expression<Func<T, bool>> predicate, QueryType queryType, EntityMap entityMap);
    }
}
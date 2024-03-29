using System;
using System.Data;
using System.Linq.Expressions;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Generator;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public interface ISqlQueryProvider
    {
        /// <summary>
        /// The type of the database
        /// </summary>
        DatabaseProvider ProviderType { get; set; }

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
        /// The char used for parameters
        /// </summary>
        string ParameterChar { get; }

        /// <summary>
        /// Build a select query
        /// </summary>
        /// <param name="selectQuery"></param>
        string BuildSelectQuery(SelectSqlQuery selectQuery);

        /// <summary>
        /// Builds an insert query
        /// </summary>
        /// <param name="insertQuery"></param>
        string BuildInsertQuery(InsertSqlQuery insertQuery);

        /// <summary>
        /// Builds an update query
        /// </summary>
        /// <param name="updateQuery"></param>
        string BuildUpdateQuery(UpdateSqlQuery updateQuery);

        /// <summary>
        /// Builds a delete query
        /// </summary>
        /// <param name="deleteQuery"></param>
        string BuildDeleteQuery(DeleteSqlQuery deleteQuery, EntityMap entityMap);

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
        /// <param name="includes"></param>
        void AppendWherePredicateQuery<T>(SqlQuery sqlQuery, Expression<Func<T, bool>> predicate, QueryType queryType, EntityMap entityMap, params Expression<Func<T, object>>[] includes);
    }
}
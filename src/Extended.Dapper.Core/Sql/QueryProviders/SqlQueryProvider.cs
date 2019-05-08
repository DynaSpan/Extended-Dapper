using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Expression;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public abstract class SqlQueryProvider : ISqlQueryProvider
    {
        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        public abstract string EscapeTable(string tableName);

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public abstract string EscapeColumn(string columnName);

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        public abstract string BuildConnectionString(DatabaseSettings databaseSettings);

        /// <summary>
        /// Generates the SQL select fields for a given entity
        /// </summary>
        /// <param name="entityMap"></param>
        /// <returns>Fields for in a SELECT query</returns>
        public virtual string GenerateSelectFields(EntityMap entityMap)
        {
            // Projection function
            string MapAliasColumn(SqlPropertyMetadata p)
            {
                if (!string.IsNullOrEmpty(p.ColumnAlias))
                    return string.Format("{0}.{1} AS {2}", 
                        this.EscapeTable(entityMap.TableName), 
                        this.EscapeColumn(p.ColumnName), 
                        this.EscapeColumn(p.PropertyName));
                else 
                    return string.Format("{0}.{1}", 
                        this.EscapeTable(entityMap.TableName), 
                        this.EscapeColumn(p.ColumnName));
            }

            return string.Join(", ", entityMap.MappedPropertiesMetadata.Select(MapAliasColumn));
        }

        /// <summary>
        /// Converts an ExpressionType to a SQL operator
        /// </summary>
        /// <param name="type"></param>
        /// <returns>SQL operator as string</returns>
        public virtual string GetSqlOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                case ExpressionType.Not:
                case ExpressionType.MemberAccess:
                    return "=";

                case ExpressionType.NotEqual:
                    return "!=";

                case ExpressionType.LessThan:
                    return "<";

                case ExpressionType.LessThanOrEqual:
                    return "<=";

                case ExpressionType.GreaterThan:
                    return ">";

                case ExpressionType.GreaterThanOrEqual:
                    return ">=";

                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    return "AND";

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";

                case ExpressionType.Default:
                    return string.Empty;

                default:
                    throw new NotSupportedException(type + " isn't supported");
            }
        }

        /// <summary>
        /// Gets a value in the correct SQL format
        /// (for LINQ string stuff such as StartsWith, Contains, etc.)
        /// </summary>
        /// <param name="methodName">Name of the LINQ method</param>
        /// <param name="value"></param>
        public virtual string GetSqlLikeValue(string methodName, object value)
        {
            if (value == null)
                value = string.Empty;

            switch (methodName)
            {
                case "StartsWith":
                    return string.Format("{0}%", value);

                case "EndsWith":
                    return string.Format("%{0}", value);

                case "StringContains":
                    return string.Format("%{0}%", value);

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the SQL selector for methodName.
        /// </summary>
        /// <param name="methodName">Name of the LINQ method</param>
        /// <param name="isNotUnary">Indicates if the selection should be 
        /// reversed (e.g. IN => NOT IN)</param>
        public virtual string GetMethodCallSqlOperator(string methodName, bool isNotUnary = false)
        {
            switch (methodName)
            {
                case "StartsWith":
                case "EndsWith":
                case "StringContains":
                    return isNotUnary ? "NOT LIKE" : "LIKE";

                case "Contains":
                    return isNotUnary ? "NOT IN" : "IN";

                case "Any":
                case "All":
                    return methodName.ToUpperInvariant();

                default:
                    throw new NotSupportedException(methodName + " isn't supported");
            }
        }

        /// <summary>
        /// Generates a where clause for a query based
        /// on the predicate
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <param name="predicate"></param>
        /// <param name="queryType"></param>
        /// <param name="entityMap"></param>
        /// TODO: refactor
        public virtual void AppendWherePredicateQuery<T>(SelectSqlQuery sqlQuery, Expression<Func<T, bool>> predicate, QueryType queryType, EntityMap entityMap)
        {
            var queryParams = new Dictionary<string, object>();

            if (predicate != null)
            {
                // WHERE
                var queryProperties = ExpressionHelper.GetQueryProperties(predicate.Body, entityMap);

                var qLevel = 0;
                var sqlBuilder = new StringBuilder();
                var conditions = new List<KeyValuePair<string, object>>();
                this.BuildQuerySql(queryProperties, entityMap, ref sqlBuilder, ref conditions, ref qLevel);

                foreach (KeyValuePair<string, object> condition in conditions)
                {
                    queryParams.Add(condition.Key, condition.Value);
                }

                // TODO implement logical deleted

                // if (entityMap.LogicalDelete && queryType == QueryType.Select)
                //     sqlQuery.Where.AppendFormat("({3}) AND {0}.{1} != {2} ", entityMap.TableName, StatusPropertyName, LogicalDeleteValue, sqlBuilder);
                // else
                    sqlQuery.Where.AppendFormat("{0} ", sqlBuilder);
            }
            else
            {
                // if (LogicalDelete && queryType == QueryType.Select)
                //     sqlQuery.SqlBuilder.AppendFormat("WHERE {0}.{1} != {2} ", TableName, StatusPropertyName, LogicalDeleteValue);
            }

            // if (LogicalDelete && HasUpdatedAt && queryType == QueryType.Delete)
            //     queryParams.Add(UpdatedAtPropertyMetadata.ColumnName, DateTime.UtcNow);

            sqlQuery.Params = queryParams;
        }

        /// <summary>
        /// Build the final `query statement and parameters`
        /// </summary>
        /// <param name="queryProperties"></param>
        /// <param name="entityMap"></param>
        /// <param name="sqlBuilder"></param>
        /// <param name="conditions"></param>
        /// <param name="qLevel">Parameters of the ranking</param>
        /// <remarks>
        /// Support `group conditions` syntax
        /// </remarks>
        /// TODO: refactor
        internal virtual void BuildQuerySql(
            IList<QueryExpression> queryProperties,
            EntityMap entityMap,
            ref StringBuilder sqlBuilder, 
            ref List<KeyValuePair<string, object>> conditions, 
            ref int qLevel)
        {
            foreach (var expr in queryProperties)
            {
                if (!string.IsNullOrEmpty(expr.LinkingOperator))
                {
                    if (sqlBuilder.Length > 0)
                        sqlBuilder.Append(" ");
                    
                    sqlBuilder
                        .Append(expr.LinkingOperator)
                        .Append(" ");
                }

                switch (expr)
                {
                    case QueryParameterExpression qpExpr:
                        var tableName = entityMap.TableName;
                        string columnName;
                        if (qpExpr.NestedProperty)
                        {
                            var joinProperty = entityMap.RelationPropertiesMetadata.First(x => x.PropertyName == qpExpr.PropertyName);
                            tableName = joinProperty.TableName;
                            columnName = joinProperty.ColumnName;
                        }
                        else
                        {
                            columnName = entityMap.MappedPropertiesMetadata.First(x => x.PropertyName == qpExpr.PropertyName).ColumnName;
                        }

                        if (qpExpr.PropertyValue == null)
                        {
                            sqlBuilder.AppendFormat("{0}.{1} {2} NULL", tableName, columnName, qpExpr.QueryOperator == "=" ? "IS" : "IS NOT");
                        }
                        else
                        {
                            var vKey = string.Format("{0}_p{1}", qpExpr.PropertyName, qLevel); //Handle multiple uses of a field
                            
                            sqlBuilder.AppendFormat("{0}.{1} {2} @{3}", tableName, columnName, qpExpr.QueryOperator, vKey);
                            conditions.Add(new KeyValuePair<string, object>(vKey, qpExpr.PropertyValue));
                        }

                        qLevel++;
                        break;

                    case QueryBinaryExpression qbExpr:
                        var nSqlBuilder = new StringBuilder();
                        var nConditions = new List<KeyValuePair<string, object>>();
                        this.BuildQuerySql(qbExpr.Nodes, entityMap, ref nSqlBuilder, ref nConditions, ref qLevel);

                        if (qbExpr.Nodes.Count == 1) //Handle `grouping brackets`
                            sqlBuilder.Append(nSqlBuilder);
                        else
                            sqlBuilder.AppendFormat("({0})", nSqlBuilder);

                        conditions.AddRange(nConditions);
                        break;
                }
            }
        }
    }
}
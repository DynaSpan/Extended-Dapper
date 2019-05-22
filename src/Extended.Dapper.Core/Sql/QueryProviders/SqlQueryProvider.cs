using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Expression;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public abstract class SqlQueryProvider : ISqlQueryProvider
    {
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        public SqlQueryProvider(DatabaseSettings databaseSettings)
        {
            this.ConnectionString = this.BuildConnectionString(databaseSettings);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public SqlQueryProvider(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

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
        /// Returns a new IDbConnection
        /// </summary>
        public abstract IDbConnection GetConnection();

        /// <summary>
        /// Build a select query
        /// </summary>
        /// <param name="selectQuery"></param>
        public virtual string BuildSelectQuery(SelectSqlQuery selectQuery)
        {
            var query = new StringBuilder();

            var selectFields = string.Join(", ", selectQuery.Select.Select(this.MapAliasColumn));
            query.AppendFormat("SELECT {0} FROM {1}", selectFields, this.EscapeTable(selectQuery.From));

            if (selectQuery.Joins != null && selectQuery.Joins.Count > 0)
            {
                query.Append(" " + string.Join(" ", selectQuery.Joins.Select(MapJoin)));
            }

            if (selectQuery.Where != null && !string.IsNullOrEmpty(selectQuery.Where.ToString()))
            {
                query.AppendFormat(" WHERE {0}", selectQuery.Where);
            }

            if (selectQuery.Limit != null)
            {
                query.AppendFormat(" LIMIT {0}", selectQuery.Limit);
            }

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(query.ToString());

            return query.ToString();
        }

        /// <summary>
        /// Builds an insert query
        /// </summary>
        /// <param name="insertQuery"></param>
        public virtual string BuildInsertQuery(InsertSqlQuery insertQuery)
        {
            var insertFields = string.Join(", ", insertQuery.Insert.Select(this.MapAliasColumn));
            var insertParams = string.Join(", ", insertQuery.Insert.Select(i => i.ParameterName));

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(string.Format("INSERT INTO {0} ({1}) VALUES ({2})", this.EscapeTable(insertQuery.Table), insertFields, insertParams));

            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", insertQuery.Table, insertFields, insertParams);
        }

        /// <summary>
        /// Builds an update query
        /// </summary>
        /// <param name="updateQuery"></param>
        public virtual string BuildUpdateQuery(UpdateSqlQuery updateQuery)
        {
            var updateFields = string.Join(", ", updateQuery.Updates.Select(x => {
                return string.Format("{0}.{1} = {2}",
                this.EscapeTable(x.Table),
                this.EscapeColumn(x.Field),
                x.ParameterName);
            }));

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(string.Format("UPDATE {0} SET {1} WHERE {2}", this.EscapeTable(updateQuery.Table), updateFields, updateQuery.Where));

            return string.Format("UPDATE {0} SET {1} WHERE {2}", this.EscapeTable(updateQuery.Table), updateFields, updateQuery.Where);
        }

        /// <summary>
        /// Builds a delete query
        /// </summary>
        /// <param name="deleteQuery"></param>
        public virtual string BuildDeleteQuery(DeleteSqlQuery deleteQuery)
        {
            // TODO clean up DeleteSqlQuery and move
            // addition of params to someplace else
            var queryBuilder = new StringBuilder();

            foreach (var param in deleteQuery.Params)
            {
                Console.WriteLine($"{param.Key} - {param.Value}");
            }

            if (deleteQuery.LogicalDelete)
            {
                queryBuilder.AppendFormat("UPDATE {0} SET {0}.{1} = 1",
                    this.EscapeTable(deleteQuery.Table),
                    this.EscapeColumn(deleteQuery.LogicalDeleteField));

                if (deleteQuery.UpdatedAtField != null && deleteQuery.UpdatedAtField != string.Empty)
                {
                    queryBuilder.AppendFormat(", {0}.{1} = @p_updatedat",
                        this.EscapeTable(deleteQuery.Table),
                        this.EscapeColumn(deleteQuery.UpdatedAtField));

                    if (!deleteQuery.Params.ContainsKey("@p_updatedat"))
                        deleteQuery.Params.Add("@p_updatedat", DateTime.UtcNow);
                }
            }
            else
            {
                queryBuilder.AppendFormat("DELETE FROM {0} ", this.EscapeTable(deleteQuery.Table));
            }

            queryBuilder.AppendFormat(" WHERE {0}.{1} NOT IN @p_id_list AND {2}.{3} = @p_parent_key",
                this.EscapeTable(deleteQuery.Table), 
                this.EscapeColumn(deleteQuery.LocalKeyField),
                this.EscapeTable(deleteQuery.ParentTable),
                this.EscapeColumn(deleteQuery.ParentKeyField));

            if (deleteQuery.LogicalDelete)
                queryBuilder.AppendFormat(" AND {0}.{1} != 1",
                    this.EscapeTable(deleteQuery.Table),
                    this.EscapeColumn(deleteQuery.LogicalDeleteField));

            if (!deleteQuery.Params.ContainsKey("@p_id_list"))
            {
                deleteQuery.Params.Add("@p_id_list", deleteQuery.DoNotErase);
                deleteQuery.Params.Add("@p_parent_key", deleteQuery.ParentKey);
            }

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(queryBuilder.ToString());

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Projection function for mapping property
        /// to SQL field
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="p"></param>
        public virtual string MapAliasColumn(SelectField selectField)
        {
            if (selectField.IsMainKey)
                return string.Format("1 AS {0}", 
                    this.EscapeColumn(selectField.Field));
            else if (!string.IsNullOrEmpty(selectField.FieldAlias))
                return string.Format("{0}.{1} AS {2}", 
                    this.EscapeTable(selectField.Table), 
                    this.EscapeColumn(selectField.Field), 
                    this.EscapeColumn(selectField.FieldAlias));
            else 
                return string.Format("{0}.{1}", 
                    this.EscapeTable(selectField.Table), 
                    this.EscapeColumn(selectField.Field));
        }

        /// <summary>
        /// Projection function for mapping property
        /// to SQL field
        /// </summary>
        /// <param name="queryField"></param>
        public virtual string MapAliasColumn(QueryField queryField)
        {
            if (!string.IsNullOrEmpty(queryField.FieldAlias))
                return string.Format("{0}.{1}", 
                    this.EscapeTable(queryField.Table), 
                    this.EscapeColumn(queryField.FieldAlias));
            else 
                return string.Format("{0}.{1}", 
                    this.EscapeTable(queryField.Table), 
                    this.EscapeColumn(queryField.Field));
        }

        /// <summary>
        /// Projecten function for mapping a join
        /// </summary>
        /// <param name="join"></param>
        public virtual string MapJoin(Join join)
        {
            var joinType = string.Empty;

            switch (join.Type)
            {
                case JoinType.INNER: joinType = "INNER"; break;
                case JoinType.LEFT: joinType = "LEFT"; break;
            }

            joinType = joinType + " JOIN";

            return string.Format("{0} {1} ON {2}.{3} = {1}.{4}",
                joinType,
                this.EscapeTable(join.ExternalTable),
                this.EscapeTable(join.LocalTable),
                this.EscapeColumn(join.LocalKey),
                this.EscapeColumn(join.ExternalKey));
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
        public virtual void AppendWherePredicateQuery<T>(SqlQuery sqlQuery, Expression<Func<T, bool>> predicate, QueryType queryType, EntityMap entityMap)
        {
            //var queryParams = new Dictionary<string, object>();

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
                    sqlQuery.Params.Add(condition.Key, condition.Value);
                }

                if (entityMap.LogicalDelete && queryType == QueryType.Select)
                    sqlQuery.Where.AppendFormat("({3}) AND {0}.{1} != {2} ", 
                        this.EscapeTable(entityMap.TableName), 
                        this.EscapeColumn(entityMap.LogicalDeletePropertyMetadata.ColumnName), 
                        1, 
                        sqlBuilder);
                else
                    sqlQuery.Where.AppendFormat("{0} ", sqlBuilder);
            }
            else
            {
                if (entityMap.LogicalDelete && queryType == QueryType.Select)
                    sqlQuery.Where.AppendFormat("{0}.{1} != {2} ", 
                        this.EscapeTable(entityMap.TableName), 
                        this.EscapeColumn(entityMap.LogicalDeletePropertyMetadata.ColumnName), 
                        1);
            }

            if (entityMap.LogicalDelete && entityMap.UpdatedAtProperty != null && queryType == QueryType.Delete)
                sqlQuery.Params.Add(entityMap.UpdatedAtPropertyMetadata.ColumnName, DateTime.UtcNow);
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
                            sqlBuilder.AppendFormat("{0}.{1} {2} NULL", 
                                this.EscapeTable(tableName), 
                                this.EscapeColumn(columnName), 
                                qpExpr.QueryOperator == "=" ? "IS" : "IS NOT");
                        }
                        else
                        {
                            var vKey = string.Format("{0}_p{1}", qpExpr.PropertyName, qLevel); //Handle multiple uses of a field
                            
                            sqlBuilder.AppendFormat("{0}.{1} {2} @{3}", 
                                this.EscapeTable(tableName), 
                                this.EscapeColumn(columnName), 
                                qpExpr.QueryOperator, 
                                vKey);

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
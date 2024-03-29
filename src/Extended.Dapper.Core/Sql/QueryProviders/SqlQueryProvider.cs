using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Database.Entities;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Generator;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Expression;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryBuilders;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public abstract class SqlQueryProvider : ISqlQueryProvider
    {
        public DatabaseProvider ProviderType { get; set; }

        protected SqlQueryProvider(DatabaseProvider dbProvider)
        {
            this.ProviderType = dbProvider;
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
        /// The char used for parameters
        /// </summary>
        public abstract string ParameterChar { get; }

        #region Select

        /// <summary>
        /// Build a select query
        /// </summary>
        /// <param name="selectQuery"></param>
        public virtual string BuildSelectQuery(SelectSqlQuery selectQuery)
        {
            var query = new StringBuilder();

            var selectFields = string.Join(", ", selectQuery.Select.Select(this.MapAliasColumn));
            query.AppendFormat("SELECT {0} FROM {1}", selectFields, this.EscapeTable(selectQuery.From));

            if (selectQuery.Joins?.Count > 0)
                query.Append(' ').AppendJoin(" ", selectQuery.Joins.Select(j => this.MapJoin(j, EntityMapper.GetEntityMap(j.EntityType))));

            if (selectQuery.Where != null && !string.IsNullOrEmpty(selectQuery.Where.ToString()))
                query.AppendFormat(" WHERE {0}", selectQuery.Where);

            if (selectQuery.OrderBy?.Count > 0)
                query.AppendFormat(" ORDER BY {0}", this.MapOrderBy(selectQuery.OrderBy));

            if (selectQuery.Limit != null)
                query.AppendFormat(" LIMIT {0}", selectQuery.Limit);

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(query.ToString());

            return query.ToString();
        }

        #endregion

        #region Insert

        /// <summary>
        /// Builds an insert query
        /// </summary>
        /// <param name="insertQuery"></param>
        public virtual string BuildInsertQuery(InsertSqlQuery insertQuery)
        {
            var insertFields = string.Join(", ", insertQuery.Insert.Select(this.MapInsertAliasColumn));
            var insertParams = string.Join(", ", insertQuery.Insert.Select(i => this.ParameterChar + i.ParameterName));

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(string.Format("INSERT INTO {0} ({1}) VALUES ({2})", this.EscapeTable(insertQuery.Table), insertFields, insertParams));

            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", this.EscapeTable(insertQuery.Table), insertFields, insertParams);
        }

        /// <summary>
        /// Projection function for mapping property
        /// to SQL field
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="p"></param>
        public virtual string MapInsertAliasColumn(QueryField selectField)
        {
            if (!string.IsNullOrEmpty(selectField.FieldAlias))
                return this.EscapeColumn(selectField.FieldAlias);
            else
                return this.EscapeColumn(selectField.Field);
        }

        #endregion

        #region Update

        /// <summary>
        /// Builds an update query
        /// </summary>
        /// <param name="updateQuery"></param>
        public virtual string BuildUpdateQuery(UpdateSqlQuery updateQuery)
        {
            var updateFields = this.MapUpdateColumn(updateQuery);

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(string.Format("UPDATE {0} SET {1} WHERE {2}", this.EscapeTable(updateQuery.Table), updateFields, updateQuery.Where));

            return string.Format("UPDATE {0} SET {1} WHERE {2}", this.EscapeTable(updateQuery.Table), updateFields, updateQuery.Where);
        }

        /// <summary>
        /// Maps the update columns for the query
        /// </summary>
        /// <param name="updateQuery"></param>
        public virtual string MapUpdateColumn(UpdateSqlQuery updateQuery) => string.Join(", ", updateQuery.Updates.Select(x => {
            return string.Format("{0}.{1} = {2}{3}",
                this.EscapeTable(x.Table),
                this.EscapeColumn(x.Field),
                this.ParameterChar,
                x.ParameterName);
        }));

        #endregion

        #region Delete

        /// <summary>
        /// Builds a delete query
        /// </summary>
        /// <param name="deleteQuery"></param>
        public virtual string BuildDeleteQuery(DeleteSqlQuery deleteQuery, EntityMap entityMap)
        {
            var queryBuilder = new StringBuilder();

            if (deleteQuery.LogicalDelete)
                this.MapLogicalDelete(deleteQuery, queryBuilder, entityMap);
            else
                queryBuilder.AppendFormat("DELETE FROM {0} ", this.EscapeTable(deleteQuery.Table));

            if (deleteQuery.DoNotErase != null && !EntityMapper.IsKeyEmpty(deleteQuery.ParentKey))
                this.LogicalDeleteAppendParentWhere(deleteQuery, queryBuilder);
            else
                queryBuilder.AppendFormat(" WHERE ({0})", deleteQuery.Where);

            if (deleteQuery.LogicalDelete)
            {
                queryBuilder.AppendFormat(" AND {0}.{1} != 1",
                    this.EscapeTable(deleteQuery.Table),
                    this.EscapeColumn(deleteQuery.LogicalDeleteField));
            }

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(queryBuilder.ToString());

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Maps a logical delete
        /// </summary>
        /// <param name="deleteQuery"></param>
        /// <param name="queryBuilder"></param>
        public virtual void MapLogicalDelete(DeleteSqlQuery deleteQuery, StringBuilder queryBuilder, EntityMap entityMap)
        {
            queryBuilder.AppendFormat("UPDATE {0} SET {0}.{1} = 1",
                this.EscapeTable(deleteQuery.Table),
                this.EscapeColumn(deleteQuery.LogicalDeleteField));

            if (!string.IsNullOrEmpty(deleteQuery.UpdatedAtField))
            {
                queryBuilder.AppendFormat(", {0}.{1} = {2}p_updatedat",
                    this.EscapeTable(deleteQuery.Table),
                    this.EscapeColumn(deleteQuery.UpdatedAtField),
                    this.ParameterChar);

                if (!deleteQuery.Params.ContainsKey("p_updatedat"))
                {
                    if (entityMap.UpdatedAtUTC)
                        deleteQuery.Params.Add(this.ParameterChar + "p_updatedat", DateTime.UtcNow);
                    else
                        deleteQuery.Params.Add(this.ParameterChar + "p_updatedat", DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Appends the where clause to a logical delete
        /// </summary>
        /// <param name="deleteQuery"></param>
        /// <param name="queryBuilder"></param>
        public virtual void LogicalDeleteAppendParentWhere(DeleteSqlQuery deleteQuery, StringBuilder queryBuilder)
        {
            queryBuilder.AppendFormat(" WHERE {0}.{1} NOT IN {2}p_id_list AND {3}.{4} = {2}p_parent_key",
                this.EscapeTable(deleteQuery.Table),
                this.EscapeColumn(deleteQuery.LocalKeyField),
                this.ParameterChar,
                this.EscapeTable(deleteQuery.ParentTable),
                this.EscapeColumn(deleteQuery.ParentKeyField));

            if (!deleteQuery.Params.ContainsKey("p_id_list"))
            {
                deleteQuery.Params.Add(this.ParameterChar + "p_id_list", deleteQuery.DoNotErase);
                deleteQuery.Params.Add(this.ParameterChar + "p_parent_key", deleteQuery.ParentKey);
            }
        }

        #endregion

        #region Generic

        /// <summary>
        /// Maps an order by on a query
        /// </summary>
        /// <param name="orderBy"></param>
        public virtual string MapOrderBy(Dictionary<SelectField, OrderBy> orderBy)
        {
            return string.Join(", ",
                orderBy.Select(o => string.Format("{0} {1}", this.MapAliasColumn(o.Key), this.MapOrderByEnum(o.Value))));
        }

        /// <summary>
        /// Maps an order by enum to the correct SQL variant
        /// </summary>
        /// <param name="orderBy"></param>
        public virtual string MapOrderByEnum(OrderBy orderBy)
        {
            return orderBy switch
            {
                OrderBy.Desc => "DESC",
                _ => "ASC",
            };
        }

        /// <summary>
        /// Projection function for mapping property
        /// to SQL field
        /// </summary>
        /// <param name="selectField"></param>
        public virtual string MapAliasColumn(SelectField selectField)
        {
            if (selectField.IsMainKey)
            {
                return string.Format("1 AS {0}", this.EscapeColumn(selectField.Field));
            }
            else if (!string.IsNullOrEmpty(selectField.FieldAlias))
            {
                if (!string.IsNullOrEmpty(selectField.TableAlias))
                {
                    return string.Format("{0}.{1} AS {2}",
                        this.EscapeTable(selectField.TableAlias),
                        this.EscapeColumn(selectField.Field),
                        this.EscapeColumn(selectField.FieldAlias));
                }
                else
                {
                    return string.Format("{0}.{1} AS {2}",
                        this.EscapeTable(selectField.Table),
                        this.EscapeColumn(selectField.Field),
                        this.EscapeColumn(selectField.FieldAlias));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(selectField.TableAlias))
                {
                    return string.Format("{0}.{1}",
                        this.EscapeTable(selectField.TableAlias),
                        this.EscapeColumn(selectField.Field));
                }
                else
                {
                    return string.Format("{0}.{1}",
                        this.EscapeTable(selectField.Table),
                        this.EscapeColumn(selectField.Field));
                }
            }
        }

        /// <summary>
        /// Projection function for mapping property
        /// to SQL field
        /// </summary>
        /// <param name="queryField"></param>
        public virtual string MapAliasColumn(QueryField queryField)
        {
            if (!string.IsNullOrEmpty(queryField.FieldAlias))
            {
                return string.Format("{0}.{1}",
                    this.EscapeTable(queryField.Table),
                    this.EscapeColumn(queryField.FieldAlias));
            }
            else
            {
                return string.Format("{0}.{1}",
                    this.EscapeTable(queryField.Table),
                    this.EscapeColumn(queryField.Field));
            }
        }

        /// <summary>
        /// Projection function for mapping a join
        /// </summary>
        /// <param name="join"></param>
        public virtual string MapJoin(Join join, EntityMap entityMap)
        {
            var joinType = string.Empty;
            var joinTable = string.Empty;

            if (join.JoinType == JoinType.LEFT)
            {
                joinType = "LEFT";
                joinTable = join.ExternalTable;
            }
            else if (join.Nullable)
            {
                joinType = "LEFT";
                joinTable = join.LocalTable;
            }
            else if (join.JoinType == JoinType.INNER)
            {
                joinType = "INNER";
                joinTable = join.LocalTable;
            }

            if (entityMap.LogicalDelete)
            {
                 return string.Format("{0} JOIN {1} {2} ON {3}.{4} = {5}.{6} AND {1}.{7} {8} 1",
                    joinType,
                    this.EscapeTable(joinTable),
                    !string.IsNullOrEmpty(join.TableAlias) ? " AS " + this.EscapeTable(join.TableAlias) : "",
                    !string.IsNullOrEmpty(join.TableAlias) ? this.EscapeTable(join.TableAlias) : this.EscapeTable(join.LocalTable),
                    this.EscapeColumn(join.LocalKey),
                    this.EscapeColumn(join.ExternalTable),
                    this.EscapeColumn(join.ExternalKey),
                    this.EscapeColumn(entityMap.LogicalDeletePropertyMetadata.ColumnName),
                    this.GetSqlOperator(ExpressionType.NotEqual));
            }
            else
            {
                return string.Format("{0} JOIN {1} {2} ON {3}.{4} = {5}.{6}",
                    joinType,
                    this.EscapeTable(joinTable),
                    !string.IsNullOrEmpty(join.TableAlias) ? " AS " + this.EscapeTable(join.TableAlias) : "",
                    !string.IsNullOrEmpty(join.TableAlias) ? this.EscapeTable(join.TableAlias) : this.EscapeTable(join.LocalTable),
                    this.EscapeColumn(join.LocalKey),
                    this.EscapeColumn(join.ExternalTable),
                    this.EscapeColumn(join.ExternalKey));
            }
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
        /// <param name="includes"></param>
        /// TODO: refactor
        public virtual void AppendWherePredicateQuery<T>(SqlQuery sqlQuery, Expression<Func<T, bool>> predicate, QueryType queryType, EntityMap entityMap, params Expression<Func<T, object>>[] includes)
        {
            if (predicate?.Body != null)
            {
                // WHERE
                var queryProperties = ExpressionHelper.GetQueryProperties(predicate.Body, entityMap, this.ProviderType);

                var qLevel = 0;
                var sqlBuilder = new StringBuilder();
                var conditions = new List<KeyValuePair<string, object>>();
                this.BuildQuerySql(queryProperties, entityMap, ref sqlBuilder, ref conditions, ref qLevel);

                foreach (KeyValuePair<string, object> condition in conditions)
                {
                    sqlQuery.Params.Add(condition.Key, condition.Value);
                }

                if (entityMap.LogicalDelete && queryType == QueryType.Select)
                {
                    sqlQuery.Where.AppendFormat("({3}) AND {0}.{1} {4} {2} ",
                        this.EscapeTable(entityMap.TableName),
                        this.EscapeColumn(entityMap.LogicalDeletePropertyMetadata.ColumnName),
                        1,
                        sqlBuilder,
                        this.GetSqlOperator(ExpressionType.NotEqual));
                }
                else
                {
                    sqlQuery.Where.AppendFormat("{0} ", sqlBuilder);
                }
            }
            else
            {
                if (entityMap.LogicalDelete && queryType == QueryType.Select)
                {
                    sqlQuery.Where.AppendFormat("{0}.{1} {2} {3} ",
                        this.EscapeTable(entityMap.TableName),
                        this.EscapeColumn(entityMap.LogicalDeletePropertyMetadata.ColumnName),
                        this.GetSqlOperator(ExpressionType.NotEqual),
                        1);
                }
            }

            if (entityMap.LogicalDelete && entityMap.UpdatedAtProperty != null && queryType == QueryType.Delete)
            {
                if (entityMap.UpdatedAtUTC)
                    sqlQuery.Params.Add(entityMap.UpdatedAtPropertyMetadata.ColumnName, DateTime.UtcNow);
                else
                    sqlQuery.Params.Add(entityMap.UpdatedAtPropertyMetadata.ColumnName, DateTime.Now);
            }
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
                        var isForeign = false;
                        var isObject = false;
                        string columnName;
                        if (qpExpr.NestedProperty)
                        {
                            var joinProperty = entityMap.RelationProperties.Where(x => x.Key.Name == qpExpr.PropertyName);
                            var truePropName = qpExpr.PropertyName.Split('.').Last();
                            var trueNestedName = qpExpr.PropertyName.Split('.')[0];

                            if (!joinProperty.Any()) // relation in nested item
                            {
                                isForeign = true;

                                joinProperty = entityMap.RelationProperties.Where(x => x.Key.Name == trueNestedName);

                                if (joinProperty.Any())
                                    isObject = joinProperty.Single().Value.Count(x => x.ExternalKey == qpExpr.PropertyName.Replace(".", "")) == 0;
                                else
                                    joinProperty = entityMap.RelationProperties.Where(x => x.Value.Select(p => p.PropertyName).Contains(truePropName));
                            }

                            var joinProp = joinProperty.First();
                            var prop = joinProp.Value.Where(p => p.PropertyName == truePropName);
                            var metadata = new SqlRelationPropertyMetadata(joinProp.Key, joinProp.Key);
                            tableName = isForeign && !isObject ? tableName : metadata.TableName;
                            columnName = isForeign ? (!isObject ? metadata.ExternalKey : prop.Single().PropertyName) : metadata.ColumnName;
                        }
                        else
                        {
                            var prop = entityMap.MappedPropertiesMetadata.FirstOrDefault(x => x.PropertyName == qpExpr.PropertyName);

                            if (prop == null) // possibly a relation
                            {
                                KeyValuePair<PropertyInfo, ICollection<SqlRelationPropertyMetadata>> joinProperty;

                                try
                                {
                                    joinProperty = entityMap.RelationProperties.FirstOrDefault(x => x.Key.Name == qpExpr.PropertyName);
                                }
                                catch
                                {
                                    throw new ArgumentException("Could not find property " + qpExpr.PropertyName);
                                }

                                var metadata = new SqlRelationPropertyMetadata(joinProperty.Key, joinProperty.Key);
                                columnName = metadata.ExternalKey;
                            } else {
                                columnName = prop.ColumnName;
                            }
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
                            var vKey = string.Format("{0}_p{1}", qpExpr.PropertyName.Replace(".", ""), qLevel); //Handle multiple uses of a field

                            sqlBuilder.AppendFormat("{0}.{1} {2} {3}{4}",
                                this.EscapeTable(tableName),
                                this.EscapeColumn(columnName),
                                qpExpr.QueryOperator,
                                this.ParameterChar,
                                vKey);

                            if (qpExpr.PropertyValue is BaseEntity)
                            {
                                var keys = EntityMapper.GetEntityKeys(qpExpr.PropertyValue, qpExpr.PropertyValue.GetType());

                                if (keys.Count() == 1)
                                    conditions.Add(new KeyValuePair<string, object>(vKey, keys.First().Value));
                                else
                                    throw new NotSupportedException("Comparing on object level is currently not supported when multiple primary keys are applied");
                            }
                            else
                            {
                                conditions.Add(new KeyValuePair<string, object>(vKey, qpExpr.PropertyValue));
                            }
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

        #endregion
    }
}
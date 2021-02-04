using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryBuilders;

namespace Extended.Dapper.Core.Sql.Generator
{
    public partial class SqlGenerator : ISqlGenerator
    {
        /// <summary>
        /// Generates a select query for an entity
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        public SelectSqlQuery Select<T>(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery  = new SelectSqlQuery();

            sqlQuery.Select.AddRange(this.GenerateSelectFields(entityMap.TableName, entityMap.MappedPropertiesMetadata));

            this.MapIncludes<T>(sqlQuery, entityMap, includes);

            // Append where
            this.sqlProvider.AppendWherePredicateQuery(sqlQuery, search, QueryType.Select, entityMap, includes);

            sqlQuery.From = entityMap.TableName;

            return sqlQuery;
        }

        /// <summary>
        /// Generates a select query from a QueryBuilder
        /// </summary>
        /// <param name="queryBuilder"></param>
        public SelectSqlQuery Select<T>(QueryBuilder<T> queryBuilder)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery = new SelectSqlQuery
            {
                Limit = queryBuilder.LimitResults
            };

            sqlQuery.Select.AddRange(this.GenerateSelectFields<T>(entityMap, queryBuilder));

            this.MapIncludes<T>(sqlQuery, entityMap, queryBuilder);
            this.MapOrderBy<T>(sqlQuery, queryBuilder, entityMap);

            var search = ExpressionHelper.CombineExpressions<T>(queryBuilder.Wheres);

            // Append where
            var includes = queryBuilder.IncludedChildren.Select(s => s.Child).ToArray();

            this.sqlProvider.AppendWherePredicateQuery<T>(sqlQuery, search, QueryType.Select, entityMap, includes);

            sqlQuery.From = entityMap.TableName;

            return sqlQuery;
        }

        /// <summary>
        /// Generates a select query for an entity
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        public SelectSqlQuery SelectForeignKey<T>(Expression<Func<T, bool>> search, string keyName)
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery  = new SelectSqlQuery();

            sqlQuery.Select.Add(
                new SelectField()
                {
                    Field = keyName,
                    Table = entityMap.TableName
                }
            );

            // Append where
            this.sqlProvider.AppendWherePredicateQuery(sqlQuery, search, QueryType.Select, entityMap);

            sqlQuery.From = entityMap.TableName;

            return sqlQuery;
        }

        /// <summary>
        /// Maps the order bys on a query
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <param name="queryBuilder"></param>
        /// <param name="entityMap"></param>
        public void MapOrderBy<T>(SelectSqlQuery sqlQuery, QueryBuilder<T> queryBuilder, EntityMap entityMap)
            where T : class
        {
            sqlQuery.OrderBy = new Dictionary<SelectField, OrderBy>();

            foreach (var orderBy in queryBuilder.OrderBys)
            {
                var orderByMember = this.GetCorrectPropertyName(orderBy.Key).ToLower();
                var sqlMetadata   = entityMap.MappedPropertiesMetadata.Single(x => x.PropertyName.ToLower() == orderByMember);

                sqlQuery.OrderBy.Add(new SelectField(){
                    IsMainKey = false,
                    Table = entityMap.TableName,
                    TableAlias = null,
                    Field = sqlMetadata.ColumnName,
                    FieldAlias = sqlMetadata.ColumnAlias
                }, orderBy.Value);
            }
        }

        public string GetCorrectPropertyName<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body is MemberExpression expression1) {
                return expression1.Member.Name;
            }
            else {
                var op = ((UnaryExpression)expression.Body).Operand;
                return ((MemberExpression)op).Member.Name;
            }
        }

        /// <summary>
        /// Generates a SQL query for selecting the manies of an entity's property
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="many"></param>
        /// <param name="search"></param>
        /// <param name="includes"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="M"></typeparam>
        public SelectSqlQuery SelectMany<T, M>(T entity, Expression<Func<T, IEnumerable<M>>> many, Expression<Func<M, bool>> search = null, params Expression<Func<M, object>>[] includes)
            where T : class
            where M : class
        {
            var manyEntityMap = EntityMapper.GetEntityMap(typeof(M));
            var rootEntityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery = this.Select<M>(search, includes);

            var manyPropertyName = ((MemberExpression)many.Body).Member.Name.ToLower();
            var manyProperty = rootEntityMap.RelationProperties.SingleOrDefault(x => x.Key.Name.ToLower() == manyPropertyName);

            var relationAttr = Attribute.GetCustomAttributes(manyProperty.Key, typeof(OneToManyAttribute), true).FirstOrDefault() as OneToManyAttribute;
            sqlQuery.Where.AppendFormat("{0} {1}.{2} = {3}o2m_parent_id",
                string.IsNullOrEmpty(sqlQuery.Where.ToString()) ? "" : " AND",
                this.sqlProvider.EscapeTable(relationAttr.TableName),
                this.sqlProvider.EscapeColumn(relationAttr.ForeignKey),
                this.sqlProvider.ParameterChar);

            sqlQuery.Params.Add("o2m_parent_id", EntityMapper.GetEntityKeys<T>(entity).First().Value);

            return sqlQuery;
        }

        /// <summary>
        /// Generates a SQL query for select a "one" entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="one"></param>
        /// <param name="includes"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="O"></typeparam>
        public SelectSqlQuery SelectOne<T, O>(T entity, Expression<Func<T, O>> one, params Expression<Func<O, object>>[] includes)
            where T : class
            where O : class
        {
            var oneEntityMap = EntityMapper.GetEntityMap(typeof(O));
            var rootEntityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery = this.Select<O>(null, includes);

            var manyPropertyName = ((MemberExpression)one.Body).Member.Name.ToLower();
            var manyProperty = rootEntityMap.RelationProperties.SingleOrDefault(x => x.Key.Name.ToLower() == manyPropertyName);

            var relationAttr = Attribute.GetCustomAttributes(manyProperty.Key, typeof(ManyToOneAttribute), true).FirstOrDefault() as ManyToOneAttribute;

            var selectRootExpr = this.CreateByIdExpression<T>(EntityMapper.GetEntityKeys<T>(entity));
            var selectRootQuery = this.SelectForeignKey<T>(selectRootExpr, relationAttr.ForeignKey);
            string rootQuery = this.sqlProvider.BuildSelectQuery(selectRootQuery);

            sqlQuery.Where.AppendFormat("{0}{1}.{2} = ({3})",
                sqlQuery.Where.ToString()?.Length == 0 ? "" : " AND ",
                this.sqlProvider.EscapeTable(oneEntityMap.TableName),
                this.sqlProvider.EscapeColumn(relationAttr.LocalKey),
                rootQuery);

            foreach (var param in selectRootQuery.Params)
                sqlQuery.Params.Add(param.Key, param.Value);

            return sqlQuery;
        }

        private ICollection<SelectField> GenerateSelectFields(string tableName, IEnumerable<SqlPropertyMetadata> properties, string tableAlias = null)
        {
            var selectList = new List<SelectField>
            {
                new SelectField()
                {
                    IsMainKey = true,
                    Table = tableName,
                    Field = "Split_" + tableName
                }
            };

            selectList.AddRange(properties.Where(p => !p.IgnoreOnSelect).Select(k =>
                new SelectField(){
                    IsMainKey = false,
                    Table = tableName,
                    TableAlias = tableAlias,
                    Field = k.ColumnName,
                    FieldAlias = k.ColumnAlias
                }
            ));

            return selectList;
        }

        private ICollection<SelectField> GenerateSelectFields<T>(EntityMap entityMap, QueryBuilder<T> queryBuilder)
            where T : class
        {
            var selectList = new List<SelectField>
            {
                new SelectField()
                {
                    IsMainKey = true,
                    Table = entityMap.TableName,
                    Field = "Split_" + entityMap.TableName
                }
            };

            List<SqlPropertyMetadata> mappedSelects = entityMap.MappedPropertiesMetadata.ToList();

            if (queryBuilder.Selects?.Count > 0)
            {
                mappedSelects = entityMap.MappedPropertiesMetadata
                    .Where(p => queryBuilder.Selects
                        .Any(f => ExpressionHelper.GetPropertyName(f) == p.PropertyName)).ToList();

                mappedSelects.AddRange(entityMap.PrimaryKeyPropertiesMetadata);
            }

            selectList.AddRange(mappedSelects.Select(k =>
                new SelectField(){
                    IsMainKey = false,
                    Table = entityMap.TableName,
                    Field = k.ColumnName,
                    FieldAlias = k.ColumnAlias
                }
            ));

            return selectList;
        }

        // private ICollection<SelectField> GenerateSelectFields<T, TChild>(EntityMap entityMap, ICollection<SqlRelationPropertyMetadata> metadata, QueryBuilder<T>.IIncludedChild child)
        //     where T : class
        //     where TChild : class
        // {
        //     var selectList = new List<SelectField>();

        //     selectList.Add(new SelectField(){
        //         IsMainKey = true,
        //         Table = entityMap.TableName,
        //         Field = "Split_" + entityMap.TableName
        //     });

        //     List<SqlPropertyMetadata> mappedSelects = metadata.Cast<SqlPropertyMetadata>().ToList();

        //     var includeChild = child as QueryBuilder<T>.IncludedChild<TChild>;

        //     if (includeChild.IncludedProperties?.Count() > 0)
        //     {
        //         mappedSelects = mappedSelects
        //             .Where(p => includeChild.IncludedProperties
        //                 .Any(f => ExpressionHelper.GetPropertyName(f) == p.PropertyName).ToList();

        //         mappedSelects.AddRange(entityMap.PrimaryKeyPropertiesMetadata);
        //     }

        //     selectList.AddRange(mappedSelects.Select(k =>
        //         new SelectField(){
        //             IsMainKey = false,
        //             Table = entityMap.TableName,
        //             Field = k.ColumnName,
        //             FieldAlias = k.ColumnAlias
        //         }
        //     ));

        //     return selectList;
        // }

        /// <summary>
        /// Maps the includes on a select query
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <param name="entityMap"></param>
        /// <param name="includes"></param>
        public void MapIncludes<T>(SelectSqlQuery sqlQuery, EntityMap entityMap, Expression<Func<T, object>>[] includes)
        {
            var relationTables = new Dictionary<string, int>();

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    var includeMemberName = ((MemberExpression)include.Body).Member.Name.ToLower();
                    var kvpProperty       = entityMap.RelationProperties.Single(x => x.Key.Name.ToLower() == includeMemberName);

                    var property = kvpProperty.Key;
                    var metadata = kvpProperty.Value;

                    var relationAttr = Attribute.GetCustomAttributes(property, typeof(RelationAttributeBase), true).FirstOrDefault() as RelationAttributeBase;

                    var tableName = relationAttr.TableName;
                    var typeStr = relationAttr.Type.ToString();

                    if (relationTables.ContainsKey(typeStr))
                        tableName = tableName + "_" + relationTables[typeStr];

                    sqlQuery.Select.AddRange(this.GenerateSelectFields(tableName, metadata.Cast<SqlPropertyMetadata>().ToList()));

                    var join = new Join()
                    {
                        EntityType = relationAttr.Type
                    };

                    // Check the type of relation
                    if (relationAttr is ManyToOneAttribute)
                    {
                        join.JoinType = JoinType.INNER;

                        join.ExternalTable = entityMap.TableName;
                        join.LocalTable = relationAttr.TableName;

                        if (relationTables.ContainsKey(typeStr))
                        {
                            join.TableAlias = $"{relationAttr.TableName}_{relationTables[typeStr]}";
                            relationTables[typeStr]++;
                        }
                        else
                        {
                            relationTables.Add(typeStr, 0);
                        }
                    }
                    else if (relationAttr is OneToManyAttribute)
                    {
                        join.JoinType = JoinType.LEFT;

                        join.ExternalTable = relationAttr.TableName;
                        join.LocalTable = entityMap.TableName;

                        if (relationTables.ContainsKey(typeStr))
                        {
                            join.TableAlias = $"{entityMap.TableName}_{relationTables[typeStr]}";
                            relationTables[typeStr]++;
                        }
                        else
                        {
                            relationTables.Add(typeStr, 0);
                        }
                    }

                    join.ExternalKey = relationAttr.ForeignKey;
                    join.LocalKey    = relationAttr.LocalKey;
                    join.Nullable    = relationAttr.Nullable;

                    sqlQuery.Joins.Add(join);
                }
            }
        }

        /// <summary>
        /// Maps the includes on a select query
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <param name="entityMap"></param>
        /// <param name="queryBuilder"></param>
        public void MapIncludes<T>(SelectSqlQuery sqlQuery, EntityMap entityMap, QueryBuilder<T> queryBuilder)
            where T : class
        {
            var relationTables = new Dictionary<string, int>();

            if (queryBuilder.IncludedChildren?.Count > 0)
            {
                foreach (var include in queryBuilder.IncludedChildren)
                {
                    var includeMemberName = include.GetMemberName().ToLower();
                    var kvpProperty       = entityMap.RelationProperties.Single(x => x.Key.Name.ToLower() == includeMemberName);

                    var property = kvpProperty.Key;
                    var metadata = kvpProperty.Value;
                    var childEntityMap = EntityMapper.GetEntityMap(include.ChildType);

                    var relationAttr = System.Attribute.GetCustomAttributes(property, typeof(RelationAttributeBase), true).FirstOrDefault() as RelationAttributeBase;

                    var tableName = relationAttr.TableName;
                    var typeStr = relationAttr.Type.ToString();

                    if (relationTables.ContainsKey(typeStr))
                        tableName = tableName + "_" + relationTables[typeStr];

                    var fields = include.GetSelectFields(childEntityMap, metadata, tableName);

                    sqlQuery.Select.AddRange(fields);

                    var join = new Join()
                    {
                        EntityType = relationAttr.Type
                    };

                    // Check the type of relation
                    if (relationAttr is ManyToOneAttribute)
                    {
                        join.JoinType = JoinType.INNER;

                        join.ExternalTable = entityMap.TableName;
                        join.LocalTable = relationAttr.TableName;

                        if (relationTables.ContainsKey(typeStr))
                        {
                            join.TableAlias = relationAttr.TableName + "_" + relationTables[typeStr];
                            relationTables[typeStr]++;
                        }
                        else
                        {
                            relationTables.Add(typeStr, 0);
                        }
                    }
                    else if (relationAttr is OneToManyAttribute)
                    {
                        join.JoinType = JoinType.LEFT;

                        join.ExternalTable = relationAttr.TableName;
                        join.LocalTable = entityMap.TableName;

                        if (relationTables.ContainsKey(typeStr))
                        {
                            join.TableAlias = entityMap.TableName + "_" + relationTables[typeStr];
                            relationTables[typeStr]++;
                        }
                        else
                        {
                            relationTables.Add(typeStr, 0);
                        }
                    }

                    join.ExternalKey = relationAttr.ForeignKey;
                    join.LocalKey    = relationAttr.LocalKey;
                    join.Nullable    = relationAttr.Nullable;

                    sqlQuery.Joins.Add(join);
                }
            }
        }
    }
}
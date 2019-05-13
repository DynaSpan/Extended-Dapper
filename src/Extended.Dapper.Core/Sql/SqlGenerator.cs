using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Extended.Dapper.Attributes.Entities;
using Extended.Dapper.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql
{
    public class SqlGenerator : ISqlGenerator
    {
        private readonly DatabaseProvider databaseProvider;
        private readonly ISqlQueryProvider sqlProvider;

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseProvider">Which database we connect to; defaults to MSSQL</param>
        public SqlGenerator(DatabaseProvider databaseProvider = DatabaseProvider.MSSQL)
        {
            this.databaseProvider = databaseProvider;
            this.sqlProvider      = SqlQueryProviderHelper.GetProvider();

            // Check if it is implemented
            if (this.sqlProvider == null)
                throw new ArgumentException(databaseProvider.ToString() + " is currently not implemented");
        }

        #endregion

        #region Insert implementation

        /// <summary>
        /// Generates an insert query for a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        public InsertSqlQuery Insert<T>(T entity)
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));

            if (entityMap.UpdatedAtProperty != null)
                entityMap.UpdatedAtProperty.SetValue(entity, DateTime.UtcNow);

            var insertQuery = new InsertSqlQuery();
            insertQuery.Table = this.sqlProvider.EscapeTable(entityMap.TableName);

            insertQuery.Insert.Append(string.Join(", ", 
                entityMap.MappedPropertiesMetadata.Select(p => this.sqlProvider.EscapeColumn(p.ColumnName))));

            insertQuery.InsertParams.Append(string.Join(", ", 
                entityMap.MappedPropertiesMetadata.Select(x => "@p_" + x.ColumnName)));

            // Get the params & values
            foreach (var metadata in entityMap.MappedPropertiesMetadata)
            {
                insertQuery.Params.Add("@p_" + metadata.ColumnName, metadata.PropertyInfo.GetValue(entity, null));
            }

            return insertQuery;
        }

        #endregion

        #region Select implementation

        /// <summary>
        /// Generates a select query for an entity
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public SelectSqlQuery Select<T>(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery  = new SelectSqlQuery();

            var joinBuilder = new StringBuilder();

            sqlQuery.Select.AddRange(this.GenerateSelectFields(entityMap.TableName, entityMap.MappedPropertiesMetadata));

            var includePropertyList = includes.Select(x => ((MemberExpression)x.Body).Member.Name.ToLower());

            if (entityMap.RelationProperties != null && entityMap.RelationProperties.Count > 0)
            {
                var relationProperties = entityMap.RelationProperties
                    .Where(x => includePropertyList.Contains(x.Key.Name.ToLower()));

                foreach (KeyValuePair<PropertyInfo, ICollection<SqlRelationPropertyMetadata>> kvpProperty in relationProperties)
                {
                    var property = kvpProperty.Key;
                    var metadata = kvpProperty.Value;

                    var relationAttr = System.Attribute.GetCustomAttributes(property, typeof(RelationAttributeBase), true).FirstOrDefault() as RelationAttributeBase;

                    sqlQuery.Select.AddRange(this.GenerateSelectFields(relationAttr.TableName, metadata.Cast<SqlPropertyMetadata>().ToList()));

                    var join = new Join();

                    // Check the type of relation
                    string joinType = string.Empty;

                    if (relationAttr is ManyToOneAttribute)
                        join.Type = JoinType.INNER;
                    else if (relationAttr is OneToManyAttribute)
                        join.Type = JoinType.LEFT;

                    join.ExternalKey = relationAttr.ExternalKey;
                    join.ExternalTable = relationAttr.TableName;

                    join.LocalKey = relationAttr.LocalKey;
                    join.LocalTable = entityMap.TableName;

                    sqlQuery.Joins.Add(join);
                }
            }

            // Append where
            if (search != null)
                this.sqlProvider.AppendWherePredicateQuery(sqlQuery, search, QueryType.Select, entityMap);

            sqlQuery.From = entityMap.TableName;

            return sqlQuery;
        }

        private ICollection<SelectField> GenerateSelectFields(string tableName, ICollection<SqlPropertyMetadata> properties)
        {
            var selectList = new List<SelectField>();

            // Add key properties first
            var keyProperties = properties.Where(x => x.PropertyInfo.GetCustomAttribute<KeyAttribute>() != null);
            var mainKeySet = false;
            Func<bool> mainKey = delegate 
            {
                if (mainKeySet) return false;

                mainKeySet = true;
                return true;
            };
            
            selectList.Add(new SelectField(){
                IsMainKey = true,
                Table = tableName,
                Field = "Split_" + tableName
            });

            selectList.AddRange(keyProperties.Select(k => 
                new SelectField(){
                    IsMainKey = false,
                    Table = tableName,
                    Field = k.ColumnName,
                    FieldAlias = k.ColumnAlias
                }
            ));

            var otherProperties = properties.Where(x => x.PropertyInfo.GetCustomAttribute<KeyAttribute>() == null);

            selectList.AddRange(otherProperties.Select(k =>
                new SelectField(){
                    IsMainKey = false,
                    Table = tableName,
                    Field = k.ColumnName,
                    FieldAlias = k.ColumnAlias
                }
            ));

            return selectList;
        }

        #endregion
    }

    public enum QueryType
    {
        Select,
        Update,
        Delete
    }
}
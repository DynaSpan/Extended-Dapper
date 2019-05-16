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
            insertQuery.Table = entityMap.TableName;

            // Grab all properties, except autovalue ones
            var autoValueProperties = entityMap.PrimaryKeyPropertiesMetadata.Where(x => x.AutoValue);

            foreach (var autoValueProperty in autoValueProperties)
            {
                var autoValueType = autoValueProperty.PropertyInfo.PropertyType;

                if (autoValueType == typeof(Guid))
                    autoValueProperty.PropertyInfo.SetValue(entity, Guid.NewGuid());
                else
                    throw new NotImplementedException($"AutoValue for type {autoValueProperty.GetType()} is not supported");
            }

            insertQuery.Insert
                .AddRange(entityMap.MappedPropertiesMetadata
                    .Where(x => !x.PropertyInfo.GetCustomAttributes<ManyToOneAttribute>().Any() 
                                && !x.PropertyInfo.GetCustomAttributes<OneToManyAttribute>().Any())
                    .Select(p => {
                        insertQuery.Params.Add("@p_" + p.ColumnName, p.PropertyInfo.GetValue(entity));

                        return new InsertField(entityMap.TableName, p.ColumnName, "@p_" + p.ColumnName, p.ColumnAlias);
                    }
            ));

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
            
            selectList.Add(new SelectField(){
                IsMainKey = true,
                Table = tableName,
                Field = "Split_" + tableName
            });

            selectList.AddRange(properties.Select(k =>
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
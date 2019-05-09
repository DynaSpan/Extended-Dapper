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
        /// <param name="predicate"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public SelectSqlQuery Select<T>(Expression<Func<T, bool>> predicate)
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlQuery  = new SelectSqlQuery();

            var joinBuilder = new StringBuilder();

            sqlQuery.Select.Append(this.sqlProvider.GenerateSelectFields(entityMap));

            if (entityMap.RelationProperties != null && entityMap.RelationProperties.Count > 0)
            {
                foreach (KeyValuePair<PropertyInfo, ICollection<SqlRelationPropertyMetadata>> kvpProperty in entityMap.RelationProperties)
                {
                    var property = kvpProperty.Key;
                    var metadata = kvpProperty.Value;

                    var relationAttr = System.Attribute.GetCustomAttributes(property, typeof(RelationAttributeBase), true).FirstOrDefault() as RelationAttributeBase;

                    var selectFields = this.sqlProvider.GenerateSelectFields(relationAttr.TableName, metadata.Cast<SqlPropertyMetadata>().ToList());

                    if (selectFields != null && selectFields != string.Empty)
                        sqlQuery.Select.AppendFormat(", {0}", selectFields);

                    // Check the type of relation
                    string joinType = string.Empty;

                    if (relationAttr is ManyToOneAttribute)
                        joinType = " INNER JOIN";
                    else if (relationAttr is OneToManyAttribute)
                        joinType = " LEFT JOIN";

                    joinBuilder.AppendFormat(" {0} {1} ON {2}.{3} = {4}.{5}",
                        joinType,
                        this.sqlProvider.EscapeTable(relationAttr.TableName),
                        this.sqlProvider.EscapeTable(entityMap.TableName),
                        this.sqlProvider.EscapeColumn(relationAttr.LocalKey),
                        this.sqlProvider.EscapeTable(relationAttr.TableName),
                        this.sqlProvider.EscapeColumn(relationAttr.ExternalKey));
                }

                sqlQuery.Joins.Append(joinBuilder.ToString());
            }

            // Append where
            if (predicate != null)
                this.sqlProvider.AppendWherePredicateQuery(sqlQuery, predicate, QueryType.Select, entityMap);

            sqlQuery.From = this.sqlProvider.EscapeTable(entityMap.TableName);

            return sqlQuery;
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
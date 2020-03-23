using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.Generator
{
    public partial class SqlGenerator : ISqlGenerator
    {
        /// <summary>
        /// Creates a delete query for a given search and entity type
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        public SqlQuery Delete<T>(Expression<Func<T, bool>> search)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));

            // TODO delete children

            // Check if it is a logical delete
            if (entityMap.LogicalDelete)
            {
                var logicalDeleteProp = entityMap.LogicalDeletePropertyMetadata;

                // Update the entity
                var query = new UpdateSqlQuery();
                query.Table = entityMap.TableName;
                query.Updates.Add(new QueryField(entityMap.TableName, logicalDeleteProp.ColumnName, "p_log_delete"));
                query.Params.Add("p_log_delete", true);

                this.sqlProvider.AppendWherePredicateQuery(query, search, QueryType.Update, entityMap);

                return query;
            }
            else 
            {
                // Delete the entity
                var query = new DeleteSqlQuery();
                query.Table = entityMap.TableName;

                this.sqlProvider.AppendWherePredicateQuery(query, search, QueryType.Delete, entityMap);

                return query;
            }
        }

        /// <summary>
        /// Generates a query for deleting the children of an entity
        /// </summary>
        /// <param name="parentTable"></param>
        /// <param name="parentKey"></param>
        /// <param name="parentKeyField"></param>
        /// <param name="localKeyField"></param>
        /// <param name="doNotErases"></param>
        /// <param name="typeOverride"></param>
        public DeleteSqlQuery DeleteChildren<T>(
            string parentTable, 
            object parentKey, 
            string parentKeyField, 
            string localKeyField, 
            List<object> doNotErases, 
            Type typeOverride = null)
            where T : class
        {
            EntityMap entityMap;

            if (typeOverride == null)
                entityMap = EntityMapper.GetEntityMap(typeof(T));
            else
                entityMap = EntityMapper.GetEntityMap(typeOverride);

            var query               = new DeleteSqlQuery();
            query.Table             = entityMap.TableName;
            query.ParentTable       = parentTable;
            query.ParentKey         = parentKey;
            query.ParentKeyField    = parentKeyField;
            query.LocalKeyField     = localKeyField;
            query.DoNotErase        = doNotErases;

            query.LogicalDelete      = entityMap.LogicalDelete;

            if (entityMap.LogicalDelete)
                query.LogicalDeleteField = entityMap.LogicalDeletePropertyMetadata.ColumnName;

            if (entityMap.UpdatedAtPropertyMetadata != null)
                query.UpdatedAtField = entityMap.UpdatedAtPropertyMetadata.ColumnName;

            return query;
        }
    }
}
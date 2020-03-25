using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.Generator
{
    public partial class SqlGenerator : ISqlGenerator
    {
        /// <summary>
        /// Generates an update query for an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="updateFields"></param>
        public UpdateSqlQuery Update<T>(T entity, Expression<Func<T, object>>[] updateFields = null)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));

            var updateQuery = new UpdateSqlQuery();
            updateQuery.Table = entityMap.TableName;

            // Grab all mapped properties
            var mappedProperties = entityMap.MappedPropertiesMetadata.Where(
                x => x.PropertyInfo.GetCustomAttribute<IgnoreOnUpdateAttribute>() == null
                     && x.PropertyInfo.GetCustomAttribute<AutoValueAttribute>() == null).ToList();

            if (entityMap.UpdatedAtProperty != null)
            {
                if (entityMap.UpdatedAtUTC)
                    entityMap.UpdatedAtProperty.SetValue(entity, DateTime.UtcNow);
                else
                    entityMap.UpdatedAtProperty.SetValue(entity, DateTime.Now);
            }

            if (updateFields != null)
            {
                mappedProperties = mappedProperties
                    .Where(p => updateFields
                        .Where(f => ExpressionHelper.GetPropertyName(f) == p.PropertyName).Any()).ToList();

                if (entityMap.UpdatedAtProperty != null)
                    mappedProperties.Add(entityMap.MappedPropertiesMetadata.Single(x => x.PropertyInfo.GetCustomAttribute<UpdatedAtAttribute>() != null));
            }

            foreach (var property in mappedProperties)
            {
                updateQuery.Updates.Add(new QueryField(entityMap.TableName, property.ColumnName, "p_" + property.ColumnName, property.ColumnAlias));
                updateQuery.Params.Add("p_" + property.ColumnName, property.PropertyInfo.GetValue(entity));
            }

            var idExpression = this.CreateByIdExpression<T>(EntityMapper.GetCompositeUniqueKey<T>(entity));
            this.sqlProvider.AppendWherePredicateQuery<T>(updateQuery, idExpression, QueryType.Update, entityMap);
            
            return updateQuery;
        }
    }
}
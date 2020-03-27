using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.Generator
{
    public partial class SqlGenerator : ISqlGenerator
    {
        /// <summary>
        /// Generates an insert query for a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeOverride"></param>
        /// <typeparam name="T"></typeparam>
        public InsertSqlQuery Insert<T>(T entity, Type typeOverride = null)
            where T : class
        {
            EntityMap entityMap;
            
            if (typeOverride != null)
                entityMap = EntityMapper.GetEntityMap(typeOverride);
            else
                entityMap = EntityMapper.GetEntityMap(typeof(T));

            if (entityMap.UpdatedAtProperty != null)
            {
                if (entityMap.UpdatedAtUTC)
                    entityMap.UpdatedAtProperty.SetValue(entity, DateTime.UtcNow);
                else
                    entityMap.UpdatedAtProperty.SetValue(entity, DateTime.Now);
            }

            var insertQuery = new InsertSqlQuery();
            insertQuery.Table = entityMap.TableName;

            // Grab all properties, except autovalue ones
            var autoValueKeyProperties = entityMap.PrimaryKeyPropertiesMetadata.Where(x => x.AutoValue);
            var autoValueProperties    = entityMap.AutoValuePropertiesMetadata;

            this.FillAutoValueProperties(entity, insertQuery, autoValueProperties, false);
            this.FillAutoValueProperties(entity, insertQuery, autoValueKeyProperties);

            insertQuery.Insert
                .AddRange(entityMap.MappedPropertiesMetadata
                    .Where(x => !x.PropertyInfo.GetCustomAttributes<RelationAttributeBase>().Any()
                        && x.PropertyInfo.GetCustomAttribute<IgnoreOnInsertAttribute>() == null
                        && ( insertQuery.AutoIncrementField == null 
                            || (insertQuery.AutoIncrementField != null && x.PropertyName != insertQuery.AutoIncrementField.PropertyName)))
                    .Select(p => {
                        insertQuery.Params.Add("p_" + p.ColumnName, p.PropertyInfo.GetValue(entity));

                        return new QueryField(entityMap.TableName, p.ColumnName, "p_" + p.ColumnName, p.ColumnAlias);
                    }
            ));

            return insertQuery;
        }

        protected void FillAutoValueProperties<T>(T entity, InsertSqlQuery insertQuery, IEnumerable<SqlPropertyMetadata> autoValueProperties, bool keyInsert = true)
        {
            foreach (var autoValueProperty in autoValueProperties)
            {
                var autoValueType = autoValueProperty.PropertyInfo.PropertyType;
                var key = autoValueProperty.PropertyInfo.GetValue(entity);

                if (keyInsert && !EntityMapper.IsKeyEmpty(key))
                    insertQuery.IdAlreadyPresent = true;
                else if (autoValueType == typeof(Guid))
                    autoValueProperty.PropertyInfo.SetValue(entity, Guid.NewGuid());
                else if (keyInsert && autoValueType == typeof(int))
                {
                    insertQuery.AutoIncrementKey = true;
                    insertQuery.AutoIncrementField = autoValueProperty as SqlKeyPropertyMetadata;
                }
                else
                    throw new NotImplementedException($"AutoValue for type {autoValueProperty.PropertyInfo.PropertyType} is not supported");
            }
        }
    }
}
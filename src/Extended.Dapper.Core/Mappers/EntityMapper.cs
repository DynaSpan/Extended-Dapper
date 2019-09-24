using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Sql.Metadata;

namespace Extended.Dapper.Core.Mappers
{
    public class EntityMapper
    {
        /// <summary>
        /// Cache entities which have been mapped
        /// </summary>
        private static readonly ConcurrentDictionary<Type, EntityMap> entityMapCache = new ConcurrentDictionary<Type, EntityMap>();

        /// <summary>
        /// Gets (or creates) a map of an entity
        /// </summary>
        /// <param name="entityType">Type of the entity</typeparam>
        public static EntityMap GetEntityMap(Type entityType)
        {
            if (entityMapCache.ContainsKey(entityType))
                return entityMapCache.Single(x => x.Key == entityType).Value;

            var entityTypeInfo  = entityType.GetTypeInfo();
            var tableAttribute  = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            var entityMap = new EntityMap();

            entityMap.Type         = entityType;
            entityMap.TableName    = tableAttribute != null ? tableAttribute.Name : entityTypeInfo.Name;
            entityMap.TableSchema  = tableAttribute != null ? tableAttribute.Schema : string.Empty;
            entityMap.Properties   = entityType.FindClassProperties().Where(q => q.CanWrite).ToArray();

            var props = entityMap.Properties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate());

            // Grab all properties with a relation
            var relationProperties = entityMap.Properties.Where(p => p.GetCustomAttributes<RelationAttributeBase>().Any());

            entityMap.RelationProperties = new Dictionary<PropertyInfo, ICollection<SqlRelationPropertyMetadata>>();

            foreach (PropertyInfo pi in relationProperties)
            {
                entityMap.RelationProperties.Add(pi, GetRelationsMetadata(pi));
            }

            // Grab all primary key properties
            var primaryKeyProperties = props.Where(p => p.GetCustomAttributes<KeyAttribute>().Any());

            entityMap.PrimaryKeyProperties          = primaryKeyProperties.ToArray();
            entityMap.PrimaryKeyPropertiesMetadata  = primaryKeyProperties.Select(p => new SqlKeyPropertyMetadata(p));

            // Grab all properties
            var properties = props.Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any());

            entityMap.MappedProperties          = properties.ToArray();
            entityMap.MappedPropertiesMetadata  = properties.Select(p => new SqlPropertyMetadata(p));

            // Grab UpdatedAt property if exists
            var updatedAtProperty = props.Where(p => p.GetCustomAttributes<UpdatedAtAttribute>().Any()).FirstOrDefault();

            if (updatedAtProperty != null 
                && (updatedAtProperty.PropertyType == typeof(DateTime) || updatedAtProperty.PropertyType == typeof(DateTime?)))
            {
                entityMap.UpdatedAtProperty         = updatedAtProperty;
                entityMap.UpdatedAtPropertyMetadata = new SqlPropertyMetadata(updatedAtProperty);
            }

            var logicalDeleteProperty = props.Where(p => p.GetCustomAttributes<DeletedAttribute>().Any()).FirstOrDefault();

            if (logicalDeleteProperty != null
                && (logicalDeleteProperty.PropertyType == typeof(bool)))
            {
                entityMap.LogicalDeleteProperty         = logicalDeleteProperty;
                entityMap.LogicalDeletePropertyMetadata = new SqlPropertyMetadata(logicalDeleteProperty);
            }

            // Add to cache
            entityMapCache.TryAdd(entityType, entityMap);

            return entityMap;
        }

        /// <summary>
        /// Generates a unique identifier for this entity
        /// </summary>
        public static object GetCompositeUniqueKey<T>(T entity)
        {
            // Get the entity map
            var entityMap = GetEntityMap(typeof(T));

            if (entityMap.PrimaryKeyPropertiesMetadata.Count() == 1)
            {
                var metadata = entityMap.PrimaryKeyPropertiesMetadata.FirstOrDefault();
                var keyVal = metadata.PropertyInfo.GetValue(entity);

                return keyVal;
            }

            var compositeKeyBuilder = new StringBuilder();

            foreach (var keyProp in entityMap.PrimaryKeyPropertiesMetadata)
            {
                var keyVal = keyProp.PropertyInfo.GetValue(entity);

                compositeKeyBuilder.AppendFormat("{0}={1};", keyProp.ColumnName, keyVal);
            }

            return compositeKeyBuilder.ToString();
        }

        /// <summary>
        /// Checks if a given key is empty
        /// </summary>
        /// <param name="value"></param>
        public static bool IsKeyEmpty(object value)
        {
            return value == null 
                || value.ToString() == Guid.Empty.ToString()
                || value.ToString() == string.Empty;
        }

        /// <summary>
        /// Maps properties with OneToMany or ManyToOne relations
        /// </summary>
        /// <param name="relationProperty">Propertiy with a RelationAttribute</param>
        private static ICollection<SqlRelationPropertyMetadata> GetRelationsMetadata(PropertyInfo relationProperty)
        {
            var propertyMetadata = new List<SqlRelationPropertyMetadata>();
            var entityType = relationProperty.PropertyType;

            // If it is a list or something that uses generics, grab
            // the "real" type
            if (entityType.IsConstructedGenericType)
                entityType = relationProperty.PropertyType.GetGenericArguments().Single();

            var relationInnerProperties = entityType.GetProperties().Where(q => q.CanWrite)
                .Where(ExpressionHelper.GetPrimitivePropertiesPredicate());

            propertyMetadata.AddRange(relationInnerProperties.Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any())
                .Select(p => new SqlRelationPropertyMetadata(relationProperty, p)));

            return propertyMetadata;
        }
    }
}
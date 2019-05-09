using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Extended.Dapper.Attributes.Entities;
using Extended.Dapper.Attributes.Entities.Relations;
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

            var props = entityMap.Properties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

            // Grab all properties with a relation
            var relationProperties = props.Where(p => 
                p.GetCustomAttributes<OneToManyAttribute>().Any() || p.GetCustomAttributes<ManyToOneAttribute>().Any()).ToArray();

            entityMap.RelationProperties         = relationProperties;
            entityMap.RelationPropertiesMetadata = GetRelationsMetadata(relationProperties);

            // Grab all primary key properties
            var primaryKeyProperties = props.Where(p => p.GetCustomAttributes<KeyAttribute>().Any());

            entityMap.PrimaryKeyProperties          = primaryKeyProperties.ToArray();
            entityMap.PrimaryKeyPropertiesMetadata  = primaryKeyProperties.Select(p => new SqlPropertyMetadata(p)).ToArray();

            // Grab all properties
            var properties = props.Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any());

            entityMap.MappedProperties          = properties.ToArray();
            entityMap.MappedPropertiesMetadata  = properties.Select(p => new SqlPropertyMetadata(p)).ToArray();

            // Grab UpdatedAt property if exists
            var updatedAtProperty = props.FirstOrDefault(p => p.GetCustomAttributes<UpdatedAtAttribute>().Count() == 1);

            if (updatedAtProperty != null 
                && (updatedAtProperty.PropertyType == typeof(DateTime) || updatedAtProperty.PropertyType == typeof(DateTime?)))
            {
                entityMap.UpdatedAtProperty         = updatedAtProperty;
                entityMap.UpdatedAtPropertyMetadata = new SqlPropertyMetadata(updatedAtProperty);
            }

            var logicalDeleteProperty = props.FirstOrDefault(p => p.GetCustomAttributes<DeletedAttribute>().Count() == 1);

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
        /// Maps properties with OneToMany or ManyToOne relations
        /// </summary>
        /// <param name="relationProperties">Properties with a RelationAttribute</param>
        private static ICollection<SqlRelationPropertyMetadata> GetRelationsMetadata(PropertyInfo[] relationProperties)
        {
            // Filter and get only non collection nested properties
            var singleJoinTypes = relationProperties.Where(p => !p.PropertyType.IsConstructedGenericType).ToArray();

            var propertyMetadata = new List<SqlRelationPropertyMetadata>();

            foreach (var propertyInfo in singleJoinTypes)
            {
                var relationInnerProperties = propertyInfo.PropertyType.GetProperties().Where(q => q.CanWrite)
                    .Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

                propertyMetadata.AddRange(relationInnerProperties.Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any())
                    .Select(p => new SqlRelationPropertyMetadata(propertyInfo, p)).ToArray());
            }

            return propertyMetadata;
        }
    }
}
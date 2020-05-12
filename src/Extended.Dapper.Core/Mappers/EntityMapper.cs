using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Keys;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Models;
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
                return entityMapCache[entityType];

            var entityTypeInfo  = entityType.GetTypeInfo();
            var tableAttribute  = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            var entityMap = new EntityMap();

            entityMap.Type         = entityType;
            entityMap.TableName    = tableAttribute?.Name ?? entityTypeInfo.Name;
            entityMap.TableSchema  = tableAttribute?.Schema ?? string.Empty;
            entityMap.Properties   = entityType.FindClassProperties().Where(q => q.CanWrite).ToArray();

            var props = entityMap.Properties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate());

            // Grab all properties with a relation
            var relationProperties = entityMap.Properties.Where(p => p.GetCustomAttributes<RelationAttributeBase>().Any());

            entityMap.RelationProperties = new Dictionary<PropertyInfo, ICollection<SqlRelationPropertyMetadata>>();
            entityMap.RelationPropertiesMetadata = relationProperties.Select(p => new SqlRelationPropertyMetadata(p, p));

            foreach (PropertyInfo pi in relationProperties)
            {
                entityMap.RelationProperties.Add(pi, GetRelationsMetadata(pi));
            }

            // Grab all primary key properties
            var primaryKeyProperties = props.Where(p => p.GetCustomAttributes<KeyAttribute>().Any());

            entityMap.PrimaryKeyProperties          = primaryKeyProperties.ToArray();
            entityMap.PrimaryKeyPropertiesMetadata  = primaryKeyProperties.Select(p => new SqlKeyPropertyMetadata(p));

            // A maximum of 1 integer autovalue key can be given to an entity
            if (entityMap.PrimaryKeyProperties.Where(p => p.PropertyType == typeof(int) && p.GetCustomAttribute<AutoValueAttribute>() != null).Count() > 1)
                throw new NotSupportedException("Multiple integer primary keys with auto value is not supported");

            // Grab all alternative key properties
            var alternativeKeyproperties = props.Where(p => p.GetCustomAttribute<AlternativeKeyAttribute>() != null);

            entityMap.AlternativeKeyProperties          = alternativeKeyproperties.ToArray();
            entityMap.AlternativeKeyPropertiesMetadata  = alternativeKeyproperties.Select(a => new SqlKeyPropertyMetadata(a));

            // Grab all autovalue properties
            var autoValueProperties = props.Where(p => p.GetCustomAttribute<KeyAttribute>() == null && p.GetCustomAttribute<AutoValueAttribute>() != null);
            entityMap.AutoValuePropertiesMetadata = autoValueProperties.Select(p => new SqlPropertyMetadata(p));

            if (entityMap.AutoValuePropertiesMetadata.Where(p => p.PropertyInfo.PropertyType == typeof(int)).Count() > 0)
                throw new NotSupportedException("Integer non-key autovalues are not supported");

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
                entityMap.UpdatedAtUTC              = updatedAtProperty.GetCustomAttribute<UpdatedAtAttribute>().UseUTC;
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
        /// Gets the keys &amp; values of the entity
        /// </summary>
        public static IEnumerable<EntityKey> GetEntityKeys<T>(T entity, Type typeOverride = null)
            where T : class
        {
            if (entity == null)
                return null;

            // Get the entity map
            EntityMap entityMap = GetEntityMap(typeOverride ?? typeof(T));

            var returnList = new List<EntityKey>();
            returnList.AddRange(entityMap.PrimaryKeyPropertiesMetadata.Select(p => new EntityKey(entity, p)));

            return returnList;
        }

        /// <summary>
        /// Gets the alternative keys &amp; values of the entity
        /// </summary>
        public static IEnumerable<EntityKey> GetAlternativeEntityKeys<T>(T entity, Type typeOverride = null)
            where T : class
        {
            if (entity == null)
                return null;

            // Get the entity map
            EntityMap entityMap = GetEntityMap(typeOverride ?? typeof(T));

            var returnList = new List<EntityKey>();
            returnList.AddRange(entityMap.AlternativeKeyPropertiesMetadata.Select(p => new EntityKey(entity, p)));

            return returnList;
        }

        /// <summary>
        /// Checks if a given key is empty
        /// </summary>
        /// <param name="value"></param>
        public static bool IsKeyEmpty(object value)
        {
            return value == null 
                || value.ToString() == Guid.Empty.ToString()
                || string.IsNullOrWhiteSpace(value.ToString())
                || value.ToString() == default(int).ToString();
        }

        /// <summary>
        /// Checks if all the autofilled values are empty
        /// </summary>
        public static bool IsAutovalueKeysEmpty<T>(T entity, Type typeOverride = null)
            where T : class
        {
            var keys  = GetEntityKeys<T>(entity, typeOverride).Where(k => k.AutoValue);
            
            // if (keys.Count() == 0)
            //     return false;

            foreach (var key in keys)
            {
                if (!EntityMapper.IsKeyEmpty(key.Value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the alternative keys are empty
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeOverride"></param>
        public static bool IsAlternativeKeysEmpty<T>(T entity, Type typeOverride = null)
            where T : class
        {
            var alternativeKeys = GetAlternativeEntityKeys<T>(entity, typeOverride);

            if (alternativeKeys.Count() == 0)
                return IsAutovalueKeysEmpty<T>(entity, typeOverride);

            foreach (var key in alternativeKeys)
            {
                if (!EntityMapper.IsKeyEmpty(key.Value))
                    return false;
            }

            return true;
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

            propertyMetadata.AddRange(relationInnerProperties
                .Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any())
                .Select(p => new SqlRelationPropertyMetadata(relationProperty, p)));

            return propertyMetadata;
        }
    }
}
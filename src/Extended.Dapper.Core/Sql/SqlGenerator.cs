using System;
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
using Extended.Dapper.Repositories.Entities;

namespace Extended.Dapper.Core.Sql
{
    public abstract class SqlGenerator<T> : ISqlGenerator where T : BaseEntity
    {
        /// <summary>
        /// Contains information about the entity
        /// </summary>
        public EntityMap EntityMap { get; set; }

        /// <summary>
        /// Contains all the properties
        /// </summary>
        public ICollection<SqlPropertyMetadata> PropertiesMetadata { get; set; }

        /// <summary>
        /// Contains all the primary key properties
        /// </summary>
        public ICollection<SqlPropertyMetadata> PrimaryKeyPropertiesMetadata { get; set; }

        /// <summary>
        /// Contains all properties with relations
        /// </summary>
        public ICollection<SqlRelationPropertyMetadata> RelationPropertiesMetadata { get; set; }

        /// <summary>
        /// Metadata of the [UpdatedAt] property (or null if none)
        /// </summary>
        public SqlPropertyMetadata UpdatedAtPropertyMetadata { get; set; }

        protected void GetMappedProperties()
        {
            var entityType      = typeof(T);
            var entityTypeInfo  = entityType.GetTypeInfo();
            var tableAttribute  = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            this.EntityMap = new EntityMap();

            this.EntityMap.TableName    = tableAttribute != null ? tableAttribute.Name : entityTypeInfo.Name;
            this.EntityMap.TableSchema  = tableAttribute != null ? tableAttribute.Schema : string.Empty;
            this.EntityMap.Properties   = entityType.FindClassProperties().Where(q => q.CanWrite).ToArray();

            var props = this.EntityMap.Properties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

            // Grab all properties with a relation
            var relationProperties = props.Where(p => 
                p.GetCustomAttributes<OneToManyAttribute>().Any() || p.GetCustomAttributes<ManyToOneAttribute>().Any()).ToArray();

            this.EntityMap.RelationProperties   = relationProperties;
            this.RelationPropertiesMetadata     = this.GetRelationsMetadata(relationProperties);

            // Grab all primary key properties
            var primaryKeyProperties = props.Where(p => p.GetCustomAttributes<KeyAttribute>().Any());

            this.EntityMap.PrimaryKeyProperties = primaryKeyProperties.ToArray();
            this.PrimaryKeyPropertiesMetadata   = primaryKeyProperties.Select(p => new SqlPropertyMetadata(p)).ToArray();

            // Grab all properties
            var properties = props.Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any());

            this.EntityMap.MappedProperties = properties.ToArray();
            this.PropertiesMetadata         = properties.Select(p => new SqlPropertyMetadata(p)).ToArray();

            // Grab UpdatedAt property if exists
            var updatedAtProperty = props.FirstOrDefault(p => p.GetCustomAttributes<UpdatedAtAttribute>().Count() == 1);

            if (updatedAtProperty != null 
                && (updatedAtProperty.PropertyType == typeof(DateTime) || updatedAtProperty.PropertyType == typeof(DateTime?)))
            {
                this.EntityMap.UpdatedAtProperty = updatedAtProperty;
                this.UpdatedAtPropertyMetadata   = new SqlPropertyMetadata(updatedAtProperty);
            }
        }

        private ICollection<SqlRelationPropertyMetadata> GetRelationsMetadata(PropertyInfo[] relationProperties)
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
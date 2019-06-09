using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Extended.Dapper.Core.Mappers;

namespace Extended.Dapper.Core.Attributes.Entities.Relations
{
    /// <summary>
    /// Base class for relation attributes
    /// </summary>
    public abstract class RelationAttributeBase : Attribute
    {
        /// <summary>
        /// Type of the property
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Name of the table
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Name of the primary key
        /// </summary>
        public string LocalKey { get; set; }

        /// <summary>
        /// Name of the foreign key
        /// </summary>
        public string ForeignKey { get; set; }

        /// <summary>
        /// Indicates if this property is nullable
        /// </summary>
        public bool Nullable { get; set; }

        public RelationAttributeBase()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type of the property (if OneToMany, just entity type)</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        public RelationAttributeBase(
            Type type,
            string foreignKey,
            bool nullable) 
            => this.Constructor(type, foreignKey, "Id", nullable);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type of the property (if OneToMany, just entity type)</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key</param>
        /// <param name="localKey">SQL COLUMN name of the local key (default: "Id")</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        public RelationAttributeBase(
            Type type,
            string foreignKey,
            string localKey = "Id",
            bool nullable = false) 
            => this.Constructor(type, foreignKey, localKey, nullable);
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type of the property (if OneToMany, just entity type)</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key</param>
        /// <param name="localKey">SQL COLUMN name of the local key (default: "Id")</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        protected virtual void Constructor(
            Type type,
            string foreignKey,
            string localKey = "Id",
            bool nullable = false) {
            if (type.IsGenericType)
                throw new ArgumentException("Type can not be a generic type");

            this.Type = type;

            var entityTypeInfo  = type.GetTypeInfo();
            var tableAttribute  = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            this.TableName = tableAttribute != null ? tableAttribute.Name : entityTypeInfo.Name;

            this.LocalKey   = localKey;
            this.ForeignKey = foreignKey;
            this.Nullable = nullable;
        }
    }
}
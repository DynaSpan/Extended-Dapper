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

        public RelationAttributeBase()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type of the property (if OneToMany, just entity type)</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key</param>
        /// <param name="localKey">SQL COLUMN name of the local key (default: "Id")</param>
        public RelationAttributeBase(
            Type type,
            string foreignKey,
            string localKey = "Id") 
        {
            if (type.IsGenericType)
                throw new ArgumentException("Type can not be a generic type");

            this.Type = type;

            var entityTypeInfo  = type.GetTypeInfo();
            var tableAttribute  = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            this.TableName = tableAttribute != null ? tableAttribute.Name : entityTypeInfo.Name;

            this.LocalKey   = localKey;
            this.ForeignKey = foreignKey;
        }
    }
}
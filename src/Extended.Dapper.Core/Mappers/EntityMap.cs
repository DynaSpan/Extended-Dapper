using System;
using System.Collections.Generic;
using System.Reflection;
using Extended.Dapper.Core.Sql.Metadata;

namespace Extended.Dapper.Core.Mappers
{
    public class EntityMap
    {
        /// <summary>
        /// The name of the table
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Schema of the table
        /// </summary>
        public string TableSchema { get; set; }

        /// <summary>
        /// The type of this entity
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Contains all properties of this entity
        /// </summary>
        public PropertyInfo[] Properties { get; set; }

        /// <summary>
        /// Contains the properties which are actually mapped and used
        /// </summary>
        public PropertyInfo[] MappedProperties { get; set; }

        /// <summary>
        /// Contains all the mapped properties
        /// </summary>
        public ICollection<SqlPropertyMetadata> MappedPropertiesMetadata { get; set; }

        /// <summary>
        /// Contains all the primary key properties
        /// </summary>
        public PropertyInfo[] PrimaryKeyProperties { get; set; }

        /// <summary>
        /// Contains all the primary key properties
        /// </summary>
        public ICollection<SqlPropertyMetadata> PrimaryKeyPropertiesMetadata { get; set; }

        /// <summary>
        /// Contains all properties with relations
        /// </summary>
        public PropertyInfo[] RelationProperties { get; set; }

        /// <summary>
        /// Contains all properties with relations
        /// </summary>
        public ICollection<SqlRelationPropertyMetadata> RelationPropertiesMetadata { get; set; }

        /// <summary>
        /// Contains the UpdatedAt property (if set; null otherwise)
        /// </summary>
        public PropertyInfo UpdatedAtProperty { get; set; }

        /// <summary>
        /// Metadata of the [UpdatedAt] property (or null if none)
        /// </summary>
        public SqlPropertyMetadata UpdatedAtPropertyMetadata { get; set; }

        /// <summary>
        /// Indicates if this entity implements a logical
        /// delete system
        /// </summary>
        /// <value>true when logical delete is implemented;
        /// false otherwise</value>
        public bool LogicalDelete => this.LogicalDeleteProperty != null;

        /// <summary>
        /// Contains the [Deleted] property (or null if none)
        /// </summary>        
        public PropertyInfo LogicalDeleteProperty { get; set; }

        /// <summary>
        /// Metadata of the [Deleted] property (or null if none)
        /// </summary>
        /// <value></value>
        public SqlPropertyMetadata LogicalDeletePropertyMetadata { get; set; }
    }
}
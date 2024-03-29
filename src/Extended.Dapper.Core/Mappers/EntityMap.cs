using System;
using System.Collections.Generic;
using System.Linq;
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
        public IEnumerable<SqlPropertyMetadata> MappedPropertiesMetadata { get; set; }

        /// <summary>
        /// Indicates if this entity has more than 1 primary key
        /// </summary>
        public bool MultipleKeys { get => this.PrimaryKeyProperties?.Count() > 1; }

        /// <summary>
        /// Contains all the primary key properties
        /// </summary>
        public PropertyInfo[] PrimaryKeyProperties { get; set; }

        /// <summary>
        /// Contains all the primary key properties
        /// </summary>
        public IEnumerable<SqlKeyPropertyMetadata> PrimaryKeyPropertiesMetadata { get; set; }

        /// <summary>
        /// Contains all the alternative key properties
        /// </summary>
        public PropertyInfo[] AlternativeKeyProperties { get; set; }

        /// <summary>
        /// Contains all the alternative key properties metadata
        /// </summary>
        public IEnumerable<SqlKeyPropertyMetadata> AlternativeKeyPropertiesMetadata {  get; set; }

        /// <summary>
        /// Contains all autovalue properties
        /// </summary>
        public IEnumerable<SqlPropertyMetadata> AutoValuePropertiesMetadata { get; set; }

        /// <summary>
        /// Contains all properties with relations
        /// </summary>
        public Dictionary<PropertyInfo, ICollection<SqlRelationPropertyMetadata>> RelationProperties { get; set; }

        /// <summary>
        /// Contains all the metadata of properties with relations
        /// </summary>
        public IEnumerable<SqlRelationPropertyMetadata> RelationPropertiesMetadata { get; set; }

        /// <summary>
        /// Contains the UpdatedAt property (if set; null otherwise)
        /// </summary>
        public PropertyInfo UpdatedAtProperty { get; set; }

        /// <summary>
        /// Metadata of the [UpdatedAt] property (or null if none)
        /// </summary>
        public SqlPropertyMetadata UpdatedAtPropertyMetadata { get; set; }

        /// <summary>
        /// Whether we should use the UTC time for updated at timestamps
        /// or local time
        /// </summary>
        public bool UpdatedAtUTC { get; set; }

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
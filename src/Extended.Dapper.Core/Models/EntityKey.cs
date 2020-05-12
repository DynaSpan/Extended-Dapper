using System;
using System.Reflection;
using Extended.Dapper.Core.Sql.Metadata;

namespace Extended.Dapper.Core.Models
{
    /// <summary>
    /// Container class for primary key values
    /// </summary>
    public class EntityKey
    {
        /// <summary>
        /// SQL name of the key
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this key is an AutoValue or not
        /// </summary>
        public bool AutoValue { get; set; }

        /// <summary>
        /// Key property
        /// </summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// The Type of the key
        /// </summary>
        public Type KeyType { get; set; }

        /// <summary>
        /// Key's value
        /// </summary>
        public object Value { get; set; }

        public EntityKey() { }

        /// <summary>
        /// Constructs the EntityKey and grabs the key's value
        /// from the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="metadata"></param>
        public EntityKey(object entity, SqlKeyPropertyMetadata metadata)
        {
            this.Name       = string.IsNullOrWhiteSpace(metadata.ColumnAlias) ? metadata.ColumnName : metadata.ColumnAlias;
            this.AutoValue  = metadata.AutoValue;
            this.Property   = metadata.PropertyInfo;
            this.KeyType    = metadata.PropertyInfo.PropertyType;
            this.Value      = this.Property.GetValue(entity);
        }

        /// <summary>
        /// Constructs the EntityKey and applies a predefined
        /// value
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="value"></param>
        public EntityKey(SqlKeyPropertyMetadata metadata, object value)
        {
            this.Name       = string.IsNullOrWhiteSpace(metadata.ColumnAlias) ? metadata.ColumnName : metadata.ColumnAlias;
            this.AutoValue  = metadata.AutoValue;
            this.Property   = metadata.PropertyInfo;
            this.KeyType    = metadata.PropertyInfo.PropertyType;
            this.Value      = value;
        }
    }
}
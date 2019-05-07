using System.Reflection;

namespace Extended.Dapper.Core.Sql
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
        /// Contains all properties of this entity
        /// </summary>
        public PropertyInfo[] Properties { get; set; }

        /// <summary>
        /// Contains the properties which are actually mapped and used
        /// </summary>
        public PropertyInfo[] MappedProperties { get; set; }

        /// <summary>
        /// Contains all the primary key properties
        /// </summary>
        public PropertyInfo[] PrimaryKeyProperties { get; set; }

        /// <summary>
        /// Contains all properties with relations
        /// </summary>
        public PropertyInfo[] RelationProperties { get; set; }

        /// <summary>
        /// Contains the UpdatedAt property (if set; null otherwise)
        /// </summary>
        public PropertyInfo UpdatedAtProperty { get; set; }
    }
}
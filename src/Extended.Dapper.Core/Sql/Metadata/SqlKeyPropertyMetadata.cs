using System;
using System.Reflection;
using Extended.Dapper.Attributes.Entities;
using Extended.Dapper.Attributes.Entities.Relations;

namespace Extended.Dapper.Core.Sql.Metadata
{
    public class SqlKeyPropertyMetadata : SqlPropertyMetadata
    {
        /// <summary>
        /// Indicates if this property uses auto value
        /// </summary>
        public bool AutoValue { get; set; }

        public SqlKeyPropertyMetadata(PropertyInfo propertyInfo) : base(propertyInfo)
        {
            this.AutoValue = false;

            var attribute = propertyInfo.GetCustomAttribute<AutoValueAttribute>();

            if (attribute != null)
            {
                // Check if type is correct
                if (propertyInfo.PropertyType != typeof(Guid))
                    throw new NotImplementedException($"Type {propertyInfo.GetType()} is not supported as AutoValue");

                this.AutoValue = true;
            }
        }
    }
}
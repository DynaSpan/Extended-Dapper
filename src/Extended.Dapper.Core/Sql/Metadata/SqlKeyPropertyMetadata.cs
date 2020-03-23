using System;
using System.Reflection;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Relations;

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
                if (propertyInfo.PropertyType != typeof(Guid) && propertyInfo.PropertyType != typeof(int))
                    throw new NotImplementedException($"Type {propertyInfo.PropertyType} is not supported as AutoValue");

                this.AutoValue = true;
            }
        }
    }
}
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Extended.Dapper.Attributes.Entities;

namespace Extended.Dapper.Core.Sql.Metadata
{
    public class SqlPropertyMetadata
    {
        /// <summary>
        /// Original PropertyInfo
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }

        /// <summary>
        /// Name of the SQL column
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Name of the column alias
        /// </summary>
        public string ColumnAlias { get; set; }

        /// <summary>
        /// Boolean indicating whether this field should
        /// be updated or not
        /// </summary>
        public bool IgnoreOnUpdate { get; set; }

        /// <summary>
        /// Name of the property
        /// </summary>
        public virtual string PropertyName => PropertyInfo.Name;

        public SqlPropertyMetadata(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo;

            // Check if an alias was set
            var alias = propertyInfo.GetCustomAttribute<ColumnAttribute>();

            if (alias != null && !string.IsNullOrEmpty(alias.Name))
            {
                this.ColumnName = alias.Name;
                this.ColumnAlias = propertyInfo.Name;
            }
            else
                this.ColumnName = propertyInfo.Name;

            // Check if we're allowed to update this
            this.IgnoreOnUpdate = propertyInfo.GetCustomAttribute<IgnoreOnUpdateAttribute>() != null;
        }
    }
}
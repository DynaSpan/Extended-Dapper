using System.Reflection;
using Extended.Dapper.Attributes.Entities.Relations;

namespace Extended.Dapper.Core.Sql.Metadata
{
    public class SqlRelationPropertyMetadata : SqlPropertyMetadata
    {
        /// <summary>
        /// Name of the table
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Name of the internal primary key
        /// </summary>
        /// <value></value>
        public string LocalKey { get; set; }

        /// <summary>
        /// Name of the external primary key
        /// </summary>
        /// <value></value>
        public string ExternalKey { get; set; }

        /// <summary>
        /// Original relation PropertyInfo
        /// </summary>
        /// <value></value>
        public PropertyInfo RelationPropertyInfo { get; set; }

        public SqlRelationPropertyMetadata(
            PropertyInfo relationPropertyInfo, 
            PropertyInfo propertyInfo) : base(propertyInfo)
        {
            var relationAttribute = 
                relationPropertyInfo.GetCustomAttribute<RelationAttributeBase>();

            this.RelationPropertyInfo = relationPropertyInfo;
            
            this.TableName      = relationAttribute.TableName;
            this.ExternalKey    = relationAttribute.ExternalKey;
            this.LocalKey       = relationAttribute.LocalKey;
        }
    }
}
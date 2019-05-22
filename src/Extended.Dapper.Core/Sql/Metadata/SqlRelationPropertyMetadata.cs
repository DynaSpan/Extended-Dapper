using System.Reflection;
using Extended.Dapper.Core.Attributes.Entities.Relations;

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
        public string LocalKey { get; set; }

        /// <summary>
        /// Name of the external primary key
        /// </summary>
        public string ExternalKey { get; set; }

        /// <summary>
        /// Original relation PropertyInfo
        /// </summary>
        public PropertyInfo RelationPropertyInfo { get; set; }

        /// <summary>
        /// The type of the relation
        /// </summary>
        public RelationType RelationType { get; set; }

        public SqlRelationPropertyMetadata(
            PropertyInfo relationPropertyInfo, 
            PropertyInfo propertyInfo) : base(propertyInfo)
        {
            var relationAttribute = 
                relationPropertyInfo.GetCustomAttribute<RelationAttributeBase>();

            this.RelationPropertyInfo = relationPropertyInfo;
            
            this.TableName      = relationAttribute.TableName;
            this.ExternalKey    = relationAttribute.ForeignKey;
            this.LocalKey       = relationAttribute.LocalKey;

            if (relationAttribute is OneToManyAttribute)
                this.RelationType = RelationType.OneToMany;
            else if (relationAttribute is ManyToOneAttribute)
                this.RelationType = RelationType.ManyToOne;
        }
    }

    public enum RelationType {
        OneToMany,
        ManyToOne
    }
}
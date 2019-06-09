using System;

namespace Extended.Dapper.Core.Attributes.Entities.Relations
{
    /// <summary>
    /// Implements a one to many relation on a field
    /// </summary>
    public sealed class OneToManyAttribute : RelationAttributeBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manyType">The type of the manies</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key (mapped in the manyType entity)</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        public OneToManyAttribute(
            Type manyType,
            string foreignKey,
            bool nullable) : base(manyType, foreignKey, "Id", nullable)
        {
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manyType">The type of the manies</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key (mapped in the manyType entity)</param>
        /// <param name="localKey">SQL COLUMN name of this entity's key (defaults to "Id")</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        public OneToManyAttribute(
            Type manyType,
            string foreignKey,
            string localKey = "Id",
            bool nullable = false) : base(manyType, foreignKey, localKey, nullable)
        {
            
        }
    }
}
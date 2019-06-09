using System;

namespace Extended.Dapper.Core.Attributes.Entities.Relations
{
    /// <summary>
    /// Implements a many to one relation on a field
    /// </summary>
    public sealed class ManyToOneAttribute : RelationAttributeBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oneType">The type of the one</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key (mapped in the manyType entity)</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        public ManyToOneAttribute(
            Type oneType,
            string foreignKey,
            bool nullable) : base(oneType, foreignKey, "Id", nullable)
        { 

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oneType">The type of the one</param>
        /// <param name="foreignKey">SQL COLUMN name of the foreign key (mapped in the manyType entity)</param>
        /// <param name="localKey">SQL COLUMN name of this entity's key (defaults to "Id")</param>
        /// <param name="nullable">Boolean indicating if the property is nullable</param>
        public ManyToOneAttribute(
            Type oneType,
            string foreignKey,
            string localKey = "Id",
            bool nullable = false) : base(oneType, foreignKey, localKey, nullable)
        { 

        }
    }
}
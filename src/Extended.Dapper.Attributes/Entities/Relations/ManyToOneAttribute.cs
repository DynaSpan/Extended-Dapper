using System;

namespace Extended.Dapper.Attributes.Entities.Relations
{
    /// <summary>
    /// Implements a many to one relation on a field
    /// </summary>
    public sealed class ManyToOneAttribute : RelationAttributeBase
    {
        public ManyToOneAttribute()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tableName">Name of the table which contains the ones</param>
        /// <param name="localKey">Name of the primary key of this object (many)</param>
        /// <param name="externalKey">Name of the primary key of the ones</param>
        public ManyToOneAttribute(
            string tableName,
            string localKey,
            string externalKey) : base(tableName, localKey, externalKey)
        { }
    }
}
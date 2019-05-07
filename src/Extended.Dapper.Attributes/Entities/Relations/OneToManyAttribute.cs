using System;

namespace Extended.Dapper.Attributes.Entities.Relations
{
    /// <summary>
    /// Implements a one to many relation on a field
    /// </summary>
    public sealed class OneToManyAttribute : RelationAttributeBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tableName">Name of the table which contains the many's</param>
        /// <param name="localKey">Name of the primary key of this object (one)</param>
        /// <param name="externalKey">Name of the primary key of the many's</param>
        public OneToManyAttribute(
            string tableName,
            string localKey,
            string externalKey) : base(tableName, localKey, externalKey)
        { }
    }
}
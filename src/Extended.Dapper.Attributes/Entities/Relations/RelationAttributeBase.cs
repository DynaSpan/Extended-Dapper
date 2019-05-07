using System;

namespace Extended.Dapper.Attributes.Entities.Relations
{
    /// <summary>
    /// Base class for relation attributes
    /// </summary>
    public abstract class RelationAttributeBase : Attribute
    {
        /// <summary>
        /// Name of the table which contains the many's
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Name of the primary key of this object (one)
        /// </summary>
        public string LocalKey { get; set; }

        /// <summary>
        /// Name of the primary key of the many's
        /// </summary>
        public string ExternalKey { get; set; }

        public RelationAttributeBase()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tableName">Name of the table which contains the many's</param>
        /// <param name="localKey">Name of the primary key of this object (one)</param>
        /// <param name="externalKey">Name of the primary key of the many's</param>
        public RelationAttributeBase(
            string tableName,
            string localKey,
            string externalKey) 
        {
            this.TableName      = tableName;
            this.LocalKey       = localKey;
            this.ExternalKey    = externalKey;
        }
    }
}
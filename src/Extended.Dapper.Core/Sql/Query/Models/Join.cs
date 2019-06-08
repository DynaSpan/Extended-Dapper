using System;

namespace Extended.Dapper.Core.Sql.Query.Models
{
    public class Join
    {
        /// <summary>
        /// Type of the join
        /// </summary>
        public JoinType JoinType { get; set; }

        /// <summary>
        /// The type of the entity this join refers to
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// Name of the external table
        /// </summary>
        public string ExternalTable { get; set; }

        /// <summary>
        /// Name of the external key
        /// </summary>
        public string ExternalKey { get; set; }

        /// <summary>
        /// Name of the local table
        /// </summary>
        public string LocalTable { get; set; }

        /// <summary>
        /// Name of the local key
        /// </summary>
        public string LocalKey { get; set; }
    }

    public enum JoinType
    {
        INNER,
        LEFT,
        RIGHT
    }
}
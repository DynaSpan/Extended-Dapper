namespace Extended.Dapper.Core.Sql.Query.Models
{
    public class Join
    {
        /// <summary>
        /// Type of the join
        /// </summary>
        public JoinType Type { get; set; }

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
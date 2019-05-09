using System.Collections.Generic;
using System.Text;

namespace Extended.Dapper.Core.Sql.Query
{
    /// <summary>
    /// POCO for storing information for generating
    /// SQL queries
    /// </summary>
    public class SqlQuery
    {
        /// <summary>
        /// Any joins that have to take place on the query
        /// </summary>
        public StringBuilder Joins { get; set; }

        /// <summary>
        /// Where clauses
        /// </summary>
        public StringBuilder Where { get; set; }

        /// <summary>
        /// Parameter values
        /// </summary>
        public Dictionary<string, object> Params { get; set; }

        public SqlQuery()
        {
            this.Params = new Dictionary<string, object>();

            this.Joins = new StringBuilder();
            this.Where = new StringBuilder();
        }
    }
}
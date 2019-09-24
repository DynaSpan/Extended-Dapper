using System;
using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql.Query
{
    /// <summary>
    /// POCO for storing information for generating
    /// SQL queries
    /// </summary>
    public abstract class SqlQuery
    {
        /// <summary>
        /// Any joins that have to take place on the query
        /// </summary>
        public List<Join> Joins { get; set; }

        /// <summary>
        /// Where clauses
        /// </summary>
        public StringBuilder Where { get; set; }

        /// <summary>
        /// Parameter values
        /// </summary>
        public Dictionary<string, object> Params { get; set; }

        /// <summary>
        /// Limit on the results returned
        /// </summary>
        public int? Limit { get; set; }

        public SqlQuery()
        {
            this.Params = new Dictionary<string, object>();

            this.Joins = new List<Join>();
            this.Where = new StringBuilder();
        }
    }
}
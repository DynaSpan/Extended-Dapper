using System;
using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.QueryProviders;

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

        /// <summary>
        /// Limit on the results returned
        /// </summary>
        public int? Limit { get; set; }

        public SqlQuery()
        {
            this.Params = new Dictionary<string, object>();

            this.Joins = new StringBuilder();
            this.Where = new StringBuilder();
        }

        public override string ToString()
        {
            if (this is SelectSqlQuery)
            {
                return SqlQueryProviderHelper.GetProvider().BuildSelectQuery(this as SelectSqlQuery);
            }

             throw new NotImplementedException();
        }
    }
}
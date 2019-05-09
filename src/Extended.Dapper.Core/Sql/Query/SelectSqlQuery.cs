using System.Collections.Generic;
using System.Text;

namespace Extended.Dapper.Core.Sql.Query
{
    public class SelectSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be selected
        /// </summary>
        public StringBuilder Select { get; set; }

        /// <summary>
        /// From which table the fields should be selected
        /// </summary>
        public string From { get; set; }

        public SelectSqlQuery() : base()
        {
            this.Select = new StringBuilder();
        }

        public override string ToString()
        {
            var query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1}", this.Select, this.From);

            if (this.Joins != null && !string.IsNullOrEmpty(this.Joins.ToString()))
            {
                query.Append(this.Joins);
            }

            if (this.Where != null && !string.IsNullOrEmpty(this.Where.ToString()))
            {
                query.AppendFormat(" WHERE {0}", this.Where);
            }

            if (this.Limit != null)
            {
                query.AppendFormat(" LIMIT {0}", this.Limit);
            }

            return query.ToString();
        }
    }
}
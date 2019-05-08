using System.Collections.Generic;
using System.Text;

namespace Extended.Dapper.Core.Sql.Query
{
    public class SelectSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be selected
        /// </summary>
        /// <value></value>
        public StringBuilder Select { get; set; }

        public SelectSqlQuery() : base()
        {
            this.Select = new StringBuilder();
        }
    }
}
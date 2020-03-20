using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql.Query
{
    public class UpdateSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be updated
        /// </summary>
        public List<QueryField> Updates { get; set; }

        /// <summary>
        /// Which table the fields should be updated
        /// </summary>
        public string Table { get; set; }

        public UpdateSqlQuery() : base()
        {
            this.Updates = new List<QueryField>();
        }
    }
}
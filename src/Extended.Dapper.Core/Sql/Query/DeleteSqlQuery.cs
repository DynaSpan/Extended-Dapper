using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.Query
{
    public class DeleteSqlQuery : SqlQuery
    {
        /// <summary>
        /// Which table the item should be deleted from
        /// </summary>
        public string Table { get; set; }

        public DeleteSqlQuery() : base()
        {
            
        }
    }
}
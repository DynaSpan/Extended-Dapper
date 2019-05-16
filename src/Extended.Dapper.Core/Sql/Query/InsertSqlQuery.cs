using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.Query
{
    public class InsertSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be inserted
        /// </summary>
        public List<InsertField> Insert { get; set; }

        /// <summary>
        /// The param names for the insert
        /// </summary>
        public StringBuilder InsertParams { get; set; }

        /// <summary>
        /// Which table the fields should be inserted
        /// </summary>
        public string Table { get; set; }

        public InsertSqlQuery() : base()
        {
            this.Insert = new List<InsertField>();
            this.InsertParams = new StringBuilder();
        }
    }
}
using System.Collections.Generic;
using System.Text;

namespace Extended.Dapper.Core.Sql.Query
{
    public class InsertSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be inserted
        /// </summary>
        public StringBuilder Insert { get; set; }

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
            this.Insert = new StringBuilder();
            this.InsertParams = new StringBuilder();
        }

        public override string ToString()
        {
            var query = new StringBuilder();
            query.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2})",
                this.Table,
                this.Insert,
                this.InsertParams);

            return query.ToString();
        }
    }
}
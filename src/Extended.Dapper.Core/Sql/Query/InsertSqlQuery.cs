using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.Query
{
    public class InsertSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be inserted
        /// </summary>
        public List<QueryField> Insert { get; set; }

        /// <summary>
        /// The param names for the insert
        /// </summary>
        public StringBuilder InsertParams { get; set; }

        /// <summary>
        /// Which table the fields should be inserted
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Boolean indicating if an id was already present,
        /// meaning this object has been inserted already
        /// </summary>
        public bool IdAlreadyPresent { get; set; }

        /// <summary>
        /// Boolean indicating if there is a AutoIncrement key,
        /// indicating we should grab the result from the insert query
        /// </summary>
        public bool AutoIncrementKey { get; set; } = false;

        /// <summary>
        /// The field that have auto increments
        /// </summary>
        public SqlKeyPropertyMetadata AutoIncrementField { get; set; }

        public InsertSqlQuery() : base()
        {
            this.Insert = new List<QueryField>();
            this.InsertParams = new StringBuilder();
        }
    }
}
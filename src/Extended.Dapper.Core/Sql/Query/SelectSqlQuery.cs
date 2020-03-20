using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryBuilder;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql.Query
{
    public class SelectSqlQuery : SqlQuery
    {
        /// <summary>
        /// The field(s) that should be selected
        /// </summary>
        public List<SelectField> Select { get; set; }

        /// <summary>
        /// From which table the fields should be selected
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// On which fields the results should be ordered
        /// </summary>
        public Dictionary<SelectField, OrderBy> OrderBy { get; set; }

        public SelectSqlQuery() : base()
        {
            this.Select = new List<SelectField>();
        }
    }
}
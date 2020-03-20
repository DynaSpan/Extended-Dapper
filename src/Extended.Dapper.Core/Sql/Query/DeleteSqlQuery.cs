using System.Collections.Generic;
using System.Text;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql.Query
{
    public class DeleteSqlQuery : SqlQuery
    {
        /// <summary>
        /// Which table the item should be deleted from
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Indicates if this delete is a logical delete
        /// or a hard one
        /// </summary>
        public bool LogicalDelete { get; set; }

        /// <summary>
        /// Name of the logical delete field
        /// </summary>
        public string LogicalDeleteField { get; set; }

        /// <summary>
        /// Name of the updated at field (if one exists)
        /// </summary>
        public string UpdatedAtField { get; set; }

        /// <summary>
        /// Used for deletion of children
        /// </summary>
        public string ParentKeyField { get; set; }

        /// <summary>
        /// Used for deletion of children
        /// </summary>
        public string ParentTable { get; set; }

        /// <summary>
        /// Used for the deletion of children
        /// </summary>
        public object ParentKey { get; set; }

        /// <summary>
        /// Used for deletion of children
        /// </summary>
        public string LocalKeyField { get; set; }

        /// <summary>
        /// Used for the deletion of children
        /// </summary>
        public List<object> DoNotErase { get; set; }

        public DeleteSqlQuery() : base()
        {
            
        }
    }
}
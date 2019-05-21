using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Repository
{
    public interface IQueryExecuter
    {
        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, IDbConnection connection = null, params Expression<Func<T, object>>[] includes)
            where T : class, new();

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <returns>true when succesful; false otherwise</returns>
       Task<bool> ExecuteInsertQuery(object entity, InsertSqlQuery query, IDbConnection connection = null);

        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        Task<bool> ExecuteUpdateQuery<T>(T entity, UpdateSqlQuery query, IDbConnection connection = null, params Expression<Func<T, object>>[] includes)
            where T : class, new();
    }
}
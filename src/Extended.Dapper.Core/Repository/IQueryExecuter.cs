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
        /// <param name="includes"></param>
        Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, params Expression<Func<T, object>>[] includes)
            where T : class;

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns>true when succesful; false otherwise</returns>
       Task<bool> ExecuteInsertQuery(object entity, InsertSqlQuery query, IDbTransaction transaction = null);

        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        Task<bool> ExecuteUpdateQuery<T>(T entity, UpdateSqlQuery query, IDbTransaction transaction = null, params Expression<Func<T, object>>[] includes)
            where T : class;

        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <returns>Number of deleted records</returns>
        Task<int> ExecuteDeleteQuery<T>(SqlQuery query, IDbTransaction transaction = null);
    }
}
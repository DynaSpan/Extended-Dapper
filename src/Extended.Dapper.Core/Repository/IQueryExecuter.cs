using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Repository
{
    public interface IQueryExecuter
    {
        /// <summary>
        /// Executes a select query by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includes"></param>
        Task<T> ExecuteSelectByIdQuery<T>(object id, params Expression<Func<T, object>>[] includes)
            where T : class;

        /// <summary>
        /// Executes a select many children query
        /// </summary>
        /// <param name="entity"></paran>
        /// <param name="many"></param>
        /// <param name="search"></param>
        /// <param name="includes"></param>
        Task<IEnumerable<M>> ExecuteSelectManyQuery<T, M>(T entity, Expression<Func<T, IEnumerable<M>>> many, Expression<Func<M, bool>> search, params Expression<Func<M, object>>[] includes)
            where T : class
            where M : class;

        /// <summary>
        /// Executes a select one children query
        /// </summary>
        /// <param name="entity"></paran>
        /// <param name="one"></param>
        /// <param name="includes"></param>
        Task<IEnumerable<O>> ExecuteSelectOneQuery<T, O>(T entity, Expression<Func<T, O>> one, params Expression<Func<O, object>>[] includes)
            where T : class
            where O : class;

        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="includes"></param>
        Task<IEnumerable<T>> ExecuteSelectQuery<T>(Expression<Func<T, bool>> search, params Expression<Func<T, object>>[] includes)
            where T : class;

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns>true when succesful; false otherwise</returns>
        Task<bool> ExecuteInsertEntityQuery<T>(T entity, IDbTransaction transaction = null, Type typeOverride = null)
            where T : class;

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <param name="queryFields"></param>
        /// <param name="queryParams"></param>
        /// <returns>true when succesful; false otherwise</returns>
        Task<bool> ExecuteInsertQuery<T>(
           T entity, 
            IDbTransaction transaction = null, 
            Type typeOverride = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null)
            where T : class;

        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        Task<bool> ExecuteUpdateQuery<T>(
            T entity, 
            IDbTransaction transaction = null, 
            Expression<Func<T, object>>[] includes = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null)
            where T : class;

        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="search"></param>
        /// <param name="transaction"></param>
        /// <returns>Number of deleted records</returns>
        Task<int> ExecuteDeleteEntityQuery<T>(T entity, IDbTransaction transaction = null)
            where T : class;

        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="search"></param>
        /// <param name="connection"></param>
        /// <returns>Number of deleted records</returns>
        Task<int> ExecuteDeleteQuery<T>(Expression<Func<T, bool>> search, IDbTransaction transaction = null)
            where T : class;
    }
}
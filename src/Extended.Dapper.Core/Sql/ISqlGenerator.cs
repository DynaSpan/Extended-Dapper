using System;
using System.Linq.Expressions;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql
{
    public interface ISqlGenerator
    {
        /// <summary>
        /// Generates an insert query for a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        InsertSqlQuery Insert<T>(T entity);

        /// <summary>
        /// Generates a select query for an entity
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        SelectSqlQuery Select<T>(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Creates an search expression for the ID
        /// </summary>
        /// <param name="id">The id that is wanted</param>
        /// <typeparam name="T">Entity type</typeparam>
        Expression<Func<T, bool>> CreateByIdExpression<T>(object id);
    }
}
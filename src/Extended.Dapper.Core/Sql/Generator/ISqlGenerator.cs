using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Extended.Dapper.Core.Models;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.QueryBuilders;

namespace Extended.Dapper.Core.Sql.Generator
{
    public interface ISqlGenerator
    {
        /// <summary>
        /// Generates an insert query for a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeOverride"></param>
        /// <typeparam name="T"></typeparam>
        InsertSqlQuery Insert<T>(T entity, Type typeOverride = null)
            where T : class;

        /// <summary>
        /// Generates a select query for an entity
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        SelectSqlQuery Select<T>(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
            where T : class;

        /// <summary>
        /// Generates a select query from a QueryBuilder
        /// </summary>
        /// <param name="queryBuilder"></param>
        SelectSqlQuery Select<T>(QueryBuilder<T> queryBuilder)
            where T : class;

        /// <summary>
        /// Generates a SQL query for selecting the manies of an entity's property
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="many"></param>
        /// <param name="search"></param>
        /// <param name="includes"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="M"></typeparam>
        SelectSqlQuery SelectMany<T, M>(T entity, Expression<Func<T, IEnumerable<M>>> many, Expression<Func<M, bool>> search = null, params Expression<Func<M, object>>[] includes)
            where T : class
            where M : class;

        /// <summary>
        /// Generates a SQL query for select a "one" entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="one"></param>
        /// <param name="includes"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="O"></typeparam>
        SelectSqlQuery SelectOne<T, O>(T entity, Expression<Func<T, O>> one, params Expression<Func<O, object>>[] includes)
            where T : class
            where O : class;

        /// <summary>
        /// Generates an update query for an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="updateFields"></param>
        UpdateSqlQuery Update<T>(T entity, Expression<Func<T, object>>[] updateFields = null)
            where T : class;

        /// <summary>
        /// Creates a delete query for a given search and entity type
        /// </summary>
        /// <param name="search"></param>
        /// <typeparam name="T"></typeparam>
        SqlQuery Delete<T>(Expression<Func<T, bool>> search)
            where T : class;

        /// <summary>
        /// Generates a query for deleting the children of an entity
        /// </summary>
        /// <param name="parentTable"></param>
        /// <param name="parentKey"></param>
        /// <param name="parentKeyField"></param>
        /// <param name="localKeyField"></param>
        /// <param name="doNotErases"></param>
        /// <param name="typeOverride"></param>
        DeleteSqlQuery DeleteChildren<T>(
            string parentTable, 
            object parentKey, 
            string parentKeyField, 
            string localKeyField, 
            List<object> doNotErases, 
            Type typeOverride = null)
            where T : class;

        /// <summary>
        /// Creates an search expression for the ID
        /// </summary>
        /// <param name="id">The id that is wanted</param>
        /// <typeparam name="T">Entity type</typeparam>
        Expression<Func<T, bool>> CreateByIdExpression<T>(object id)
            where T : class;

        /// <summary>
        /// Creates an search expression for the ID
        /// </summary>
        /// <param name="keys">The keys to search ofor</param>
        /// <typeparam name="T">Entity type</typeparam>
        Expression<Func<T, bool>> CreateByIdExpression<T>(IEnumerable<EntityKey> keys)
            where T : class;
    }
}
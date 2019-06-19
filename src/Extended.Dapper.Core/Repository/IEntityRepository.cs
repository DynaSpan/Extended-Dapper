using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Extended.Dapper.Core.Repository
{
    public interface IEntityRepository<T> where T : class
    {
        /// <summary>
        /// Gets all the entities
        /// </summary>
        /// <param name="includes">Which children to include</param>
        /// <returns>List of entities</returns>
        Task<IEnumerable<T>> GetAll(params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets one or more entities that match the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>List of entities</returns>
        Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> search, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets one entity that matches the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>A single entity that matches the search</returns>
        Task<T> Get(Expression<Func<T, bool>> search, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>The entity that matches the ID</param>
        Task<T> GetById(object id, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Retrieves the many of an entity
        /// </summary>
        /// <param name="many">The many property of the entity</param>
        /// <param name="includes">Which children should be included in the manies</param>
        /// <typeparam name="M"></typeparam>
        /// <returns>A list with manies</returns>
        Task<IEnumerable<M>> GetMany<M>(T entity, Expression<Func<T, IEnumerable<M>>> many, params Expression<Func<M, object>>[] includes)
            where M : class;

        /// <summary>
        /// Retrieves the many of an entity
        /// </summary>
        /// <param name="many">The many property of the entity</param>
        /// <param name="search">A LINQ query to filter the children</param>
        /// <param name="includes">Which children should be included in the manies</param>
        /// <typeparam name="M"></typeparam>
        /// <returns>A list with manies</returns>
        Task<IEnumerable<M>> GetMany<M>(T entity, Expression<Func<T, IEnumerable<M>>> many, Expression<Func<M, bool>> search, params Expression<Func<M, object>>[] includes)
            where M : class;

        /// <summary>
        /// Retrieves a one of an entity
        /// </summary>
        /// <param name="one">The one property of the entity</param>
        /// <param name="includes">Which children should be included in the child</param>
        /// <typeparam name="O"></typeparam>
        /// <returns>An instance of the child</returns>
        Task<O> GetOne<O>(T entity, Expression<Func<T, O>> one, params Expression<Func<O, object>>[] includes)
            where O : class;

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>The inserted entity</returns>
        Task<T> Insert(T entity);

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>The inserted entity</returns>
        Task<T> Insert(T entity, IDbTransaction transaction);

        /// <summary>
        /// Updates a given entity (but won't update any children info)
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>True when succesful; false otherwise</returns>
        Task<bool> Update(T entity);

        /// <summary>
        /// Updates a given entity (but won't update any children info)
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>True when succesful; false otherwise</returns>
        Task<bool> Update(T entity, IDbTransaction transaction);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        /// <returns>True when succesful; false otherwise</returns>
        Task<bool> Update(T entity, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="transaction">Database transaction</param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        /// <returns>True when succesful; false otherwise</returns>
        Task<bool> Update(T entity, IDbTransaction transaction, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Deletes the given entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <returns>Number of deleted rows</returns>
        Task<int> Delete(T entity);

        /// <summary>
        /// Deletes the given entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>Number of deleted rows</returns>
        Task<int> Delete(T entity, IDbTransaction transaction);

        /// <summary>
        /// Deletes the entities matching the search
        /// </summary>
        /// <param name="search">Search for items to delete</param>
        /// <returns>Number of deleted rows</returns>
        Task<int> Delete(Expression<Func<T, bool>> search);

        /// <summary>
        /// Deletes the entities matching the search
        /// </summary>
        /// <param name="search">Search for items to delete</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>Number of deleted rows</returns>
        Task<int> Delete(Expression<Func<T, bool>> search, IDbTransaction transaction);
    }
}
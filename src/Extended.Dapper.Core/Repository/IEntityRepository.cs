using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Extended.Dapper.Core.Repository
{
    public interface IEntityRepository<T> where T : class
    {
        /// <summary>
        /// Gets one or more entities that match the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets one entity that matches the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        Task<T> Get(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        Task<T> GetById(object id, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity"></param>
        Task<T> Insert(T entity);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        Task<bool> Update(T entity, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Deletes the given entity
        /// </summary>
        /// <param name="entity"></param>
        Task<int> Delete(T entity);

        /// <summary>
        /// Deletes the entities matching the search
        /// </summary>
        /// <param name="search"></param>
        Task<int> Delete(Expression<Func<T, bool>> search);
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Extended.Dapper.Core.Repository
{
    public interface IEntityRepository<T>
    {
        /// <summary>
        /// Gets one or more entities that match the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        Task<IEnumerable<T>> Get(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        Task<T> GetById(Guid id, params Expression<Func<T, object>>[] includes);

        
    }
}
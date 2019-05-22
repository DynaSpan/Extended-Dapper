using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Database.Entities;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Repository
{
    public class EntityRepository<T> : IEntityRepository<T> where T : class
    {
        protected IDatabaseFactory DatabaseFactory { get; set; }
        protected SqlGenerator SqlGenerator { get; set; }
        protected IQueryExecuter QueryExecuter { get; set; }

        public EntityRepository(IDatabaseFactory databaseFactory, IQueryExecuter queryExecuter = null)
        {
            this.DatabaseFactory = databaseFactory;
            this.SqlGenerator    = new SqlGenerator(databaseFactory.DatabaseProvider);

            if (queryExecuter == null)
                this.QueryExecuter = new QueryExecuter(databaseFactory, this.SqlGenerator);
            else
                this.QueryExecuter = queryExecuter;
        }

        /// <summary>
        /// Gets one or more entities that match the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        public virtual Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
        {
            var query = this.SqlGenerator.Select<T>(search, includes);

            return this.QueryExecuter.ExecuteSelectQuery(query, null, includes);
        }

        /// <summary>
        /// Gets one entity that matches the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        public virtual async Task<T> Get(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
        {
            var query = this.SqlGenerator.Select<T>(search, includes);

            return (await this.QueryExecuter.ExecuteSelectQuery(query, null, includes)).FirstOrDefault();
        }

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        public virtual Task<T> GetById(object id, params Expression<Func<T, object>>[] includes)
        {
            var search = this.SqlGenerator.CreateByIdExpression<T>(id);

            return this.Get(search, includes);
        }

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity"></param>
        public virtual async Task<T> Insert(T entity)
        {
            var query = this.SqlGenerator.Insert<T>(entity);

            if (await this.QueryExecuter.ExecuteInsertQuery(entity, query))
                return entity;

            return null;
        }

        /// <summary>
        /// Updates a given entity (but won't update any children info)
        /// </summary>
        /// <param name="entity"></param>
        public virtual Task<bool> Update(T entity) => this.Update(entity, null);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        public virtual Task<bool> Update(T entity, params Expression<Func<T, object>>[] includes)
        {
            var query = this.SqlGenerator.Update<T>(entity);

            return this.QueryExecuter.ExecuteUpdateQuery<T>(entity, query, null, includes);
        }

        /// <summary>
        /// Deletes the given entity
        /// </summary>
        /// <param name="entity"></param>
        public virtual Task<int> Delete(T entity)
        {
            var entityId = EntityMapper.GetCompositeUniqueKey<T>(entity);
            var search = this.SqlGenerator.CreateByIdExpression<T>(entityId);

            return this.Delete(search);
        }

        /// <summary>
        /// Deletes the entities matching the search
        /// </summary>
        /// <param name="search"></param>
        public virtual Task<int> Delete(Expression<Func<T, bool>> search)
        {
            var query = this.SqlGenerator.Delete<T>(search);

            return this.QueryExecuter.ExecuteDeleteQuery<T>(query);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Sql.Generator;
using Extended.Dapper.Core.Sql.QueryBuilders;
using Extended.Dapper.Core.Sql.QueryExecuter;

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
        /// Gets a new instance of the QueryBuilder
        /// </summary>
        public virtual QueryBuilder<T> GetQueryBuilder()
            => new QueryBuilder<T>(this.QueryExecuter);

        /// <summary>
        /// Gets all the entities
        /// </summary>
        /// <param name="includes">Which children to include</param>
        /// <returns>List of entities</returns>
        public virtual Task<IEnumerable<T>> GetAll(params Expression<Func<T, object>>[] includes)
            => this.GetAll(null, includes);

        /// <summary>
        /// Gets one or more entities that match the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>List of entities</returns>
        public virtual Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> search, params Expression<Func<T, object>>[] includes)
            => this.QueryExecuter.ExecuteSelectQuery<T>(search, includes);

        /// <summary>
        /// Gets one entity that matches the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>A single entity that mataches the search</returns>
        public virtual async Task<T> Get(Expression<Func<T, bool>> search, params Expression<Func<T, object>>[] includes)
            => (await this.QueryExecuter.ExecuteSelectQuery<T>(search, includes).ConfigureAwait(false))?.FirstOrDefault();

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>The entity that matches the ID</param>
        public virtual Task<T> GetById(object id, params Expression<Func<T, object>>[] includes)
            => this.QueryExecuter.ExecuteSelectByIdQuery<T>(id, includes);

        /// <summary>
        /// Gets an entity by its alternative ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        /// <returns>The entity that matches the ID</param>
        public virtual Task<T> GetByAlternativeId(object id, params Expression<Func<T, object>>[] includes)
            => this.QueryExecuter.ExecuteSelectByAlternativeIdQuery<T>(id, includes);

        /// <summary>
        /// Retrieves the many of an entity
        /// </summary>
        /// <param name="many">The many property of the entity</param>
        /// <param name="includes">Which children should be included in the manies</param>
        /// <typeparam name="M"></typeparam>
        /// <returns>A list with manies</returns>
        public virtual Task<IEnumerable<M>> GetMany<M>(T entity, Expression<Func<T, IEnumerable<M>>> many, params Expression<Func<M, object>>[] includes)
            where M : class => this.GetMany(entity, many, null, includes);

        /// <summary>
        /// Retrieves the many of an entity
        /// </summary>
        /// <param name="many">The many property of the entity</param>
        /// <param name="search">A LINQ query to filter the children</param>
        /// <param name="includes">Which children should be included in the manies</param>
        /// <typeparam name="M"></typeparam>
        /// <returns>A list with manies</returns>
        public virtual Task<IEnumerable<M>> GetMany<M>(T entity, Expression<Func<T, IEnumerable<M>>> many, Expression<Func<M, bool>> search, params Expression<Func<M, object>>[] includes)
            where M : class => this.QueryExecuter.ExecuteSelectManyQuery<T, M>(entity, many, search, includes);

        /// <summary>
        /// Retrieves a one of an entity
        /// </summary>
        /// <param name="one">The one property of the entity</param>
        /// <param name="includes">Which children should be included in the child</param>
        /// <typeparam name="O"></typeparam>
        /// <returns>An instance of the child</returns>
        public virtual async Task<O> GetOne<O>(T entity, Expression<Func<T, O>> one, params Expression<Func<O, object>>[] includes)
            where O : class => (await this.QueryExecuter.ExecuteSelectOneQuery<T, O>(entity, one, includes).ConfigureAwait(false))?.FirstOrDefault();

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>The inserted entity</returns>
        public virtual Task<T> Insert(T entity) 
            => this.Insert(entity, null, false);

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>The inserted entity</returns>
        public virtual Task<T> Insert(T entity, IDbTransaction transaction) 
            => this.Insert(entity, transaction, false);

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <param name="forceInsert">Forces Extended.Dapper to insert an item where autovalue key is filled</param>
        /// <returns>The inserted entity</returns>
        public virtual Task<T> Insert(T entity, bool forceInsert) 
            => this.Insert(entity, null, forceInsert);

        /// <summary>
        /// Inserts an entity into the database
        /// Also inserts the children if no ID is set
        /// on them
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <param name="transaction">Database transaction</param>
        /// <param name="forceInsert">Forces Extended.Dapper to insert an item where autovalue key is filled</param>
        /// <returns>The inserted entity</returns>
        public virtual async Task<T> Insert(T entity, IDbTransaction transaction, bool forceInsert)
        {
            if (await this.QueryExecuter.ExecuteInsertEntityQuery<T>(entity, transaction, null, forceInsert).ConfigureAwait(false))
                return entity;

            return null;
        }

        /// <summary>
        /// Updates a given entity (but won't update any children info)
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>True when succesful; false otherwise</returns>
        public virtual Task<bool> Update(T entity) 
            => this.Update(null, null, entity);

        /// <summary>
        /// Updates a given entity (but won't update any children info)
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>True when succesful; false otherwise</returns>
        public virtual Task<bool> Update(T entity, IDbTransaction transaction) 
            => this.Update(transaction, null, entity);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        /// <returns>True when succesful; false otherwise</returns>
        public virtual Task<bool> Update(T entity, params Expression<Func<T, object>>[] includes) 
            => this.Update(null, includes, entity);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="transaction">Database transaction</param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        /// <returns>True when succesful; false otherwise</returns>
        public virtual Task<bool> Update(T entity, IDbTransaction transaction, params Expression<Func<T, object>>[] includes)
            => this.Update(transaction, includes, entity);

        /// <summary>
        /// Updates a given entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="transaction">Database transaction</param>
        /// <param name="includes">Which children should also be updated
        /// (erases them if they don't exist in the list anymore)</param>
        /// <returns>True when succesful; false otherwise</returns>
        protected virtual Task<bool> Update(IDbTransaction transaction, Expression<Func<T, object>>[] includes, T entity)
            => this.QueryExecuter.ExecuteUpdateQuery<T>(entity, transaction, null, includes);

        /// <summary>
        /// Updates only the provided fields on an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="updateFields"></param>
        /// <returns>True when succesful; false otherwise</returns>
        public virtual Task<bool> UpdateOnly(T entity, params Expression<Func<T, object>>[] updateFields)
            => this.UpdateOnly(entity, null, updateFields);

        /// <summary>
        /// Updates only the provided fields on an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <param name="updateFields"></param>
        /// <returns>True when succesful; false otherwise</returns>
        public virtual Task<bool> UpdateOnly(T entity, IDbTransaction transaction, params Expression<Func<T, object>>[] updateFields)
            => this.QueryExecuter.ExecuteUpdateQuery<T>(entity, transaction, updateFields);

        /// <summary>
        /// Deletes the given entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <returns>Number of deleted rows</returns>
        public virtual Task<int> Delete(T entity)
            => this.Delete(entity, null);

        /// <summary>
        /// Deletes the given entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>Number of deleted rows</returns>
        public virtual Task<int> Delete(T entity, IDbTransaction transaction)
            => this.QueryExecuter.ExecuteDeleteEntityQuery(entity, transaction);

        /// <summary>
        /// Deletes the entities matching the search
        /// </summary>
        /// <param name="search">Search for items to delete</param>
        /// <returns>Number of deleted rows</returns>
        public virtual Task<int> Delete(Expression<Func<T, bool>> search)
            => this.Delete(search, null);

        /// <summary>
        /// Deletes the entities matching the search
        /// </summary>
        /// <param name="search">Search for items to delete</param>
        /// <param name="transaction">Database transaction</param>
        /// <returns>Number of deleted rows</returns>
        public virtual Task<int> Delete(Expression<Func<T, bool>> search, IDbTransaction transaction)
            => this.QueryExecuter.ExecuteDeleteQuery<T>(search, transaction);
    }
}
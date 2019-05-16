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
    public class EntityRepository<T> : IEntityRepository<T> where T : class, new()
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
        public virtual Task<IEnumerable<T>> Get(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
        {
            var query = this.SqlGenerator.Select<T>(search, includes);

            return this.QueryExecuter.ExecuteSelectQuery(query, null, includes);
        }

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        public virtual async Task<T> GetById(Guid id, params Expression<Func<T, object>>[] includes)
        {
            // TODO implement
            return null;
        }


        public virtual async Task<T> Insert(T entity)
        {
            var query = this.SqlGenerator.Insert<T>(entity);

            if (await this.QueryExecuter.ExecuteInsertQuery(entity, query))
                return entity;

            return null;
        }
    }
}
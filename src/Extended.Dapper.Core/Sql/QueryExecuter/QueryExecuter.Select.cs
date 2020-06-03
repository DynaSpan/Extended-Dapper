using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Models;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql.Generator;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.QueryBuilders;

namespace Extended.Dapper.Core.Sql.QueryExecuter
{
    public partial class QueryExecuter : IQueryExecuter
    {
        /// <summary>
        /// Executes a query built by the query builder
        /// and returns the results
        /// </summary>
        /// <param name="queryBuilder"></param>
        public virtual Task<IEnumerable<T>> ExecuteQueryBuilder<T>(QueryBuilder<T> queryBuilder)
            where T : class
        {
            var query = this.SqlGenerator.Select<T>(queryBuilder);

            return this.ExecuteSelectQuery<T>(query, queryBuilder.IncludedChildren.Select(s => s.Child).ToArray());
        }

        /// <summary>
        /// Executes a select query by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includes"></param>
        public virtual async Task<T> ExecuteSelectByIdQuery<T>(object id, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var search = this.SqlGenerator.CreateByIdExpression<T>(id);

            return (await this.ExecuteSelectQuery<T>(search, includes)).FirstOrDefault();
        }

        /// <summary>
        /// Executes a select query by alternative id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includes"></param>
        public virtual async Task<T> ExecuteSelectByAlternativeIdQuery<T>(object id, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));

            if (entityMap.AlternativeKeyPropertiesMetadata.Count() == 0)
                throw new NotSupportedException("Could not get by alternative key when entity has none defined");

            if (entityMap.AlternativeKeyPropertiesMetadata.Count() > 1)
                throw new NotSupportedException("Could not get by single alternative key when entity has multiple defined");

            var key = new EntityKey(entityMap.AlternativeKeyPropertiesMetadata.First(), id);

            var search = this.SqlGenerator.CreateByIdExpression<T>(new List<EntityKey>() { key });

            return (await this.ExecuteSelectQuery<T>(search, includes)).FirstOrDefault();
        }

        /// <summary>
        /// Executes a select many children query
        /// </summary>
        /// <param name="entity"></paran>
        /// <param name="many"></param>
        /// <param name="search"></param>
        /// <param name="includes"></param>
        public virtual Task<IEnumerable<M>> ExecuteSelectManyQuery<T, M>(T entity, Expression<Func<T, IEnumerable<M>>> many, Expression<Func<M, bool>> search, params Expression<Func<M, object>>[] includes)
            where T : class
            where M : class
        {
            var query = this.SqlGenerator.SelectMany<T, M>(entity, many, search, includes);

            return this.ExecuteSelectQuery<M>(query, includes);
        }

        /// <summary>
        /// Executes a select one children query
        /// </summary>
        /// <param name="entity"></paran>
        /// <param name="one"></param>
        /// <param name="includes"></param>
        public virtual Task<IEnumerable<O>> ExecuteSelectOneQuery<T, O>(T entity, Expression<Func<T, O>> one, params Expression<Func<O, object>>[] includes)
            where T : class
            where O : class
        {
            var query = this.SqlGenerator.SelectOne<T, O>(entity, one, includes);

            return this.ExecuteSelectQuery<O>(query, includes);
        }

        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="search"></param>
        /// <param name="includes"></param>
        public virtual Task<IEnumerable<T>> ExecuteSelectQuery<T>(Expression<Func<T, bool>> search, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var query = this.SqlGenerator.Select<T>(search, includes);

            return this.ExecuteSelectQuery<T>(query, includes);
        }

        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="includes"></param>
        public virtual Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, params Expression<Func<T, object>>[] includes)
            where T : class
            => this.ExecuteSelectQuery(query, null, includes);

        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <param name="includes"></param>
        public virtual async Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, IDbTransaction transaction = null, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var typeArr = ReflectionHelper.GetTypeListFromIncludes<T>(includes).ToArray();
            string selectQuery = this.DatabaseFactory.SqlProvider.BuildSelectQuery(query);
            
            // Grab keys
            var keys = query.Select.Where(x => x.IsMainKey).ToList();
            keys.Remove(keys.First()); // remove first key as it is from entity itself

            string splitOn = string.Join(",", keys.Select(k => k.Field)).Trim();

            var entityLookup = new Dictionary<string, T>();

            IDbConnection connection = null;

            if (transaction == null) 
            {
                connection = this.DatabaseFactory.GetDatabaseConnection();
                this.OpenConnection(connection);
            } else {
                connection = transaction.Connection;
            }

            try
            {
                this.OpenConnection(connection);

                await connection.QueryAsync<T>(selectQuery, typeArr, DapperMapper.MapDapperEntity<T>(typeArr, entityLookup, includes), query.Params, transaction, true, splitOn);
                
                if (transaction == null)
                    connection?.Close();
            }
            catch (Exception)
            {
                connection?.Close();
                connection?.Dispose();

                throw;
            }

            return entityLookup.Values.ToArray();
        }

        public virtual async Task<object> GetEntityByAlternativeKey<T>(T entity, Type typeOverride = null, IDbTransaction transaction = null)
            where T : class
        {
            if (typeOverride != null)
            {
                // TODO test
                var reflectionCall = ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "GetEntityByAlternativeKey", typeOverride, new object[] { entity, null, transaction }, this) as Task<object>;
                
                return await reflectionCall;

                // var resultProperty = reflectionCall.GetType().GetProperty("Result");
                // return resultProperty.GetValue(reflectionCall);
            }

            var expression = this.SqlGenerator.CreateByIdExpression<T>(EntityMapper.GetAlternativeEntityKeys<T>(entity));
            var idQuery = this.SqlGenerator.Select<T>(expression);
            var objEntity = (await this.ExecuteSelectQuery<T>(idQuery, transaction)).FirstOrDefault();

            return objEntity;
        }

        public virtual async Task<IEnumerable<EntityKey>> GetEntityKeysFromAlternativeKeys(object entity, Type entityType, IDbTransaction transaction = null)
        {
            var dbEntity = await this.GetEntityByAlternativeKey(entity, entityType, transaction);
            var keys = EntityMapper.GetEntityKeys(dbEntity, entityType);

            if (dbEntity != null && keys != null)
            {
                foreach (var key in keys)
                    key.Property.SetValue(entity, key.Value);
            }

            return keys;
        }
    }
}
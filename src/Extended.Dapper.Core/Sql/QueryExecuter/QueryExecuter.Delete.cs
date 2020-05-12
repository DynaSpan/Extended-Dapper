using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Models;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryExecuter
{
    public partial class QueryExecuter : IQueryExecuter
    {
        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="search"></param>
        /// <param name="transaction"></param>
        /// <returns>Number of deleted records</returns>
        public virtual Task<int> ExecuteDeleteEntityQuery<T>(T entity, IDbTransaction transaction = null)
            where T : class
        {
            IEnumerable<EntityKey> entityId;

            if (!EntityMapper.IsAutovalueKeysEmpty(entity))
                entityId = EntityMapper.GetEntityKeys<T>(entity);
            else if (!EntityMapper.IsAlternativeKeysEmpty(entity))
                entityId = EntityMapper.GetAlternativeEntityKeys<T>(entity);
            else
                throw new IndexOutOfRangeException("Could not find unique key to execute this update");

            var search = this.SqlGenerator.CreateByIdExpression<T>(entityId);

            return this.ExecuteDeleteQuery<T>(search, transaction);
        }

        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="search"></param>
        /// <param name="transaction"></param>
        /// <returns>Number of deleted records</returns>
        public virtual async Task<int> ExecuteDeleteQuery<T>(Expression<Func<T, bool>> search, IDbTransaction transaction = null)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var query = this.SqlGenerator.Delete<T>(search);
            var shouldCommit = false;
            IDbConnection connection = null;

            if (transaction == null) 
            {
                connection = this.DatabaseFactory.GetDatabaseConnection();
                this.OpenConnection(connection);

                transaction = connection.BeginTransaction();
                shouldCommit = true;
            }

            try
            {
                string queryStr;

                if (entityMap.LogicalDelete)
                    queryStr = this.DatabaseFactory.SqlProvider.BuildUpdateQuery((UpdateSqlQuery)query);
                else
                    queryStr = this.DatabaseFactory.SqlProvider.BuildDeleteQuery((DeleteSqlQuery)query, entityMap);
                
                var result = await transaction.Connection.ExecuteAsync(queryStr, query.Params, transaction);

                if (shouldCommit)
                    transaction.Commit();

                connection?.Close();

                return result;
            }
            catch (Exception)
            {
                try {
                    transaction?.Rollback();
                } catch (Exception) { }

                connection?.Close();

                throw;
            }
            finally
            {
                connection?.Close();
            }
        }
    }
}
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Sql.QueryExecuter
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
            var entityId = EntityMapper.GetCompositeUniqueKey<T>(entity);
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
                string deleteQuery = this.DatabaseFactory.SqlProvider.BuildDeleteQuery(query as DeleteSqlQuery);
                var result = await transaction.Connection.ExecuteAsync(deleteQuery, query.Params, transaction);

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
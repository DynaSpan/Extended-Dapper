using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Database.Entities;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql;

namespace Extended.Dapper.Core.EntityRepository
{
    public class EntityRepository<T> : IEntityRepository<T> where T : BaseEntity
    {
        protected IDatabaseFactory DatabaseFactory { get; set; }
        protected SqlGenerator SqlGenerator { get; set; }

        public EntityRepository(IDatabaseFactory databaseFactory)
        {
            this.DatabaseFactory = databaseFactory;
            this.SqlGenerator = new SqlGenerator(databaseFactory.DatabaseProvider);
        }

        public virtual Task<IEnumerable<T>> Get(Expression<Func<T, bool>> search = null)
        {
            return Task.Factory.StartNew<IEnumerable<T>>(() => {
                using (var connection = this.DatabaseFactory.GetDatabaseConnection())
                {
                    this.OpenConnection(connection);

                    var query = this.SqlGenerator.Select<T>(search);

                    var result = connection.Query<T>(query.ToString(), query.Params);

                    return result;
                }
            });
        }

        public virtual Task<IEnumerable<T>> Get(Expression<Func<T, bool>> search = null, Func<object[], T> includes = null)
        {
            return Task.Factory.StartNew<IEnumerable<T>>(() => {
                using (var connection = this.DatabaseFactory.GetDatabaseConnection())
                {
                    this.OpenConnection(connection);

                    var query = this.SqlGenerator.Select<T>(search);

                    var typeArr = ReflectionHelper.GetTypeListFromIncludes(includes).ToArray();

                    connection.QueryAsync<T>(query.ToString(), typeArr, includes);


                    //var dapperQuery = DapperHelper.CreateDapperQuery(connection, query.ToString(), includes);

                    return dapperQuery;
                }
            });
        }

        protected virtual void OpenConnection(IDbConnection connection)
        {
            connection.Open();

            if (connection.State != System.Data.ConnectionState.Open)
                throw new ApplicationException("Could not connect to the SQL server");
        }
    }
}
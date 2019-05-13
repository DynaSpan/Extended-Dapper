using System;
using System.Collections.Generic;
using System.Data;
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

namespace Extended.Dapper.Core.Repository
{
    public class EntityRepository<T> : IEntityRepository<T> where T : BaseEntity, new()
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

        public virtual async Task<IEnumerable<T>> Get(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
        {
            using (var connection = this.DatabaseFactory.GetDatabaseConnection())
            {
                this.OpenConnection(connection);

                var query = this.SqlGenerator.Select<T>(search);

                var typeArr = ReflectionHelper.GetTypeListFromIncludes(includes).ToArray();

                //connection.QueryAsync<T>(query.ToString(), typeArr, includes);


                //var dapperQuery = DapperHelper.CreateDapperQuery(connection, query.ToString(), includes);

                Console.WriteLine(query.ToString());

                // Grab keys
                var keys = query.Select.Where(x => x.IsMainKey).ToList();
                //keys.Remove(keys.First()); // remove first key as it is from entity itself

                string splitOn = string.Join(",", keys.Select(k => k.Field));
                Console.WriteLine(splitOn);

                return await connection.QueryAsync<T>(query.ToString(), typeArr, this.MapDapperEntity(typeArr, includes), null, null, true, splitOn);

                // return dapperQuery;
            }
        }

        protected virtual Func<object[], T> MapDapperEntity(Type[] typeArr, params Expression<Func<T, object>>[] includes)
        {
            return (objectArr) => {
                var entity = objectArr[0] as T;
                var entityMap = EntityMapper.GetEntityMap(typeof(T));

                Array.ForEach(includes, incl => {
                    var type = incl.Body.Type.GetTypeInfo();
                    object value = null;

                    var exp = (MemberExpression)incl.Body;
                    var property = entityMap.RelationProperties.Where(x => x.Key.Name == exp.Member.Name).SingleOrDefault();

                    if (type.IsGenericType)
                    {
                        // Handle as list, get all entities with this type
                        var listType = type.GetGenericArguments()[0].GetTypeInfo();

                        MethodInfo method = typeof(ReflectionHelper).GetMethod("CloneListAs");
                        MethodInfo genericMethod = method.MakeGenericMethod(listType);

                        var objList = objectArr.Where(x => x.GetType() == listType).ToList();
                        value = genericMethod.Invoke(null, new[] { objList });
                    }
                    else
                    {
                        // Handle as single object
                        value = objectArr.Where(x => x.GetType() == type).FirstOrDefault();
                    }

                    if (property.Key != null && value != null)
                        property.Key.SetValue(entity, value);
                });

                // for (int i = 1; i < objectArr.Length; i++)
                // {
                //     var obj = objectArr[i];
                //     var objType = obj.GetType().GetTypeInfo();
                    
                //     // Find the correct include
                //     var incl = includes.Where(inc => inc.Body.Type.GetTypeInfo() == objType).FirstOrDefault();

                //     if (incl != null)
                //     {
                //         var exp = (MemberExpression)incl.Body;
                //         var member = exp.Member.Name;
                //         //var memberName = incl;
                //         //string memberName = incl.Body.Member.Name;

                        
                //         property.Key.SetValue(entity, obj);

                //         //var test = incl.Compile().Invoke(entity) as T;
                //         //var correspondingObject = objectArr.Where(o => o.GetType().GetTypeInfo() == incl.Body.Type.GetTypeInfo());
                //         //Console.WriteLine(test);
                //     }
                // }

                // Array.ForEach(includes, incl => {
                    

                //     if (correspondingObject != null)
                //     {
                        
                //     }
                // });

                return entity;
            };
        }

        protected virtual void OpenConnection(IDbConnection connection)
        {
            connection.Open();

            if (connection.State != System.Data.ConnectionState.Open)
                throw new ApplicationException("Could not connect to the SQL server");
        }
    }
}
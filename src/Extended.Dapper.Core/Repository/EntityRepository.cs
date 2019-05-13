using System;
using System.Collections;
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
using Extended.Dapper.Core.Sql.Query;

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

        /// <summary>
        /// Gets one or more entities that match the search
        /// </summary>
        /// <param name="search">The search criteria</param>
        /// <param name="includes">Which children to include</param>
        public virtual async Task<IEnumerable<T>> Get(Expression<Func<T, bool>> search = null, params Expression<Func<T, object>>[] includes)
        {
            using (var connection = this.DatabaseFactory.GetDatabaseConnection())
            {
                this.OpenConnection(connection);

                var query = this.SqlGenerator.Select<T>(search, includes);
                
                return await this.ExecuteSelectQuery(query, connection, includes);
            }
        }

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="includes">Which children to include</param>
        public virtual async Task<T> GetById(Guid id, params Expression<Func<T, object>>[] includes)
        {
            using (var connection = this.DatabaseFactory.GetDatabaseConnection())
            {
                this.OpenConnection(connection);

                Expression<Func<T, bool>> search = (entity => entity.Id == id);

                var query  = this.SqlGenerator.Select<T>(search, includes);
                var result = await this.ExecuteSelectQuery(query, connection, includes);
                
                if (result != null)
                    return result.FirstOrDefault();

                return null;
            }
        }


        public virtual async Task<T> Insert(T entity)
        {
            using (var connection = this.DatabaseFactory.GetDatabaseConnection())
            {
                this.OpenConnection(connection);

                entity.Id = Guid.NewGuid(); // set ID for entity

                var query = this.SqlGenerator.Insert<T>(entity);
                await this.ExecuteInsertQuery(query, connection);

                var entityMap = EntityMapper.GetEntityMap(typeof(T));

                // Get all relations
                foreach (var relation in entityMap.RelationProperties)
                {
                    var property = relation.Key;
                    var metadata = relation.Value.Where(x => x.PropertyName == property.Name).FirstOrDefault();
                    var value = property.GetValue(entity);

                    if (value != null)
                    {
                        if (property.GetType().IsGenericType)
                        {
                            // Collection
                            var objMap = EntityMapper.GetEntityMap(property.GetType().GetGenericArguments()[0].GetTypeInfo());
                        }
                        else
                        {
                            // Normal object
                            var objMap = EntityMapper.GetEntityMap(property.GetType());

                            if (objMap != null)
                            {
                                // Set value of external key
                                var insertQuery = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", property.GetType(), new[] { value }) as InsertSqlQuery;
                                insertQuery.Insert.AppendFormat(", {0}", metadata.ExternalKey);
                                insertQuery.InsertParams.AppendFormat(", @{0}_ExtParentId", metadata.TableName);
                                insertQuery.Params.Add(string.Format("@{0}_ExtParentId", metadata.TableName), entity.Id);

                                await this.ExecuteInsertQuery(query, connection);
                            }
                        }
                    }
                }

                return entity;
            }
        }

        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        protected virtual async Task<IEnumerable<T>> ExecuteSelectQuery(SelectSqlQuery query, IDbConnection connection, params Expression<Func<T, object>>[] includes)
        {
            var typeArr = ReflectionHelper.GetTypeListFromIncludes(includes).ToArray();
            
            // Grab keys
            var keys = query.Select.Where(x => x.IsMainKey).ToList();
            keys.Remove(keys.First()); // remove first key as it is from entity itself

            string splitOn = string.Join(",", keys.Select(k => k.Field));

            var entityLookup = new Dictionary<Guid, T>();

            await connection.QueryAsync<T>(query.ToString(), typeArr, this.MapDapperEntity(typeArr, entityLookup, includes), query.Params, null, true, splitOn);

            return entityLookup.Values;
        }

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <returns>true when succesful; false otherwise</returns>
        protected virtual async Task<bool> ExecuteInsertQuery(InsertSqlQuery query, IDbConnection connection)
        {
            var insertResult = await connection.ExecuteAsync(query.ToString(), query.Params);

            return insertResult == 1;
        }

        /// <summary>
        /// Maps a query result set to an entity
        /// </summary>
        /// <param name="typeArr">Array with all sub-types</param>
        /// <param name="lookup">Dictionary to lookup entities</param>
        /// <param name="includes">Which children should be included</param>
        protected virtual Func<object[], T> MapDapperEntity(Type[] typeArr, Dictionary<Guid, T> lookup, params Expression<Func<T, object>>[] includes)
        {
            return (objectArr) => {
                var entityLookup = objectArr[0] as T;
                T entity;

                if (!lookup.TryGetValue(entityLookup.Id, out entity))
                {
                    lookup.Add(entityLookup.Id, entity = entityLookup);
                }

                var entityMap = EntityMapper.GetEntityMap(typeof(T));

                Array.ForEach(includes, incl => {
                    var type = incl.Body.Type.GetTypeInfo();

                    var exp = (MemberExpression)incl.Body;
                    var property = entityMap.RelationProperties.Where(x => x.Key.Name == exp.Member.Name).SingleOrDefault();

                    if (type.IsGenericType)
                    {
                        // Handle as list, get all entities with this type
                        var listType     = type.GetGenericArguments()[0].GetTypeInfo();
                        var listProperty = property.Key.GetValue(entity) as IList;

                        var objList = objectArr.Where(x => x.GetType() == listType).ToList();
                        IList value = ReflectionHelper.CastListTo(listType, objList);

                        if (value != null)
                        {
                            if (listProperty == null)
                                property.Key.SetValue(entity, value);
                            else 
                                foreach (var val in value)
                                    listProperty.Add(val);
                        }
                    }
                    else
                    {
                        // Handle as single object
                        object value = objectArr.Where(x => x.GetType() == type).FirstOrDefault();

                        if (property.Key != null && value != null)
                            property.Key.SetValue(entity, value);
                    }
                });

                return entity;
            };
        }

        /// <summary>
        /// Opens the provided connection
        /// </summary>
        /// <param name="connection"></param>
        protected virtual void OpenConnection(IDbConnection connection)
        {
            connection.Open();

            if (connection.State != System.Data.ConnectionState.Open)
                throw new ApplicationException("Could not connect to the SQL server");
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Attributes.Entities;
using Extended.Dapper.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Repository
{
    public class QueryExecuter : IQueryExecuter
    {
        protected IDatabaseFactory DatabaseFactory { get; set; }
        protected ISqlGenerator SqlGenerator { get; set; }

        public QueryExecuter(IDatabaseFactory databaseFactory, ISqlGenerator sqlGenerator)
        {
            this.DatabaseFactory = databaseFactory;
            this.SqlGenerator = sqlGenerator;
        }

        /// <summary>
        /// Executes a select query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        public virtual async Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, IDbConnection connection = null, params Expression<Func<T, object>>[] includes)
            where T : class, new()
        {
            var typeArr = ReflectionHelper.GetTypeListFromIncludes(includes).ToArray();
            
            // Grab keys
            var keys = query.Select.Where(x => x.IsMainKey).ToList();
            keys.Remove(keys.First()); // remove first key as it is from entity itself

            string splitOn = string.Join(",", keys.Select(k => k.Field));

            var entityLookup = new Dictionary<string, T>();

            if (connection == null) 
                connection = this.DatabaseFactory.GetDatabaseConnection();

            using (connection)
            {
                this.OpenConnection(connection);

                await connection.QueryAsync<T>(query.ToString(), typeArr, this.MapDapperEntity(typeArr, entityLookup, includes), query.Params, null, true, splitOn);
            }

            return entityLookup.Values;
        }

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <returns>true when succesful; false otherwise</returns>
        public virtual async Task<bool> ExecuteInsertQuery(object entity, InsertSqlQuery query, IDbConnection connection = null)
        {
            if (connection == null) 
                connection = this.DatabaseFactory.GetDatabaseConnection();

            // First grab & insert all the ManyToOnes (foreign keys of entity)
            query = await this.InsertManyToOnes(entity, query);

            using (connection)
            {
                this.OpenConnection(connection);

                var insertResult = await connection.ExecuteAsync(query.ToString(), query.Params);

                if (insertResult == 1)
                {
                    var entityKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", entity.GetType(), new[] { entity }) as string;

                    // Grab the OneToManys
                    await this.InsertOneToManys(entity, entityKey);
                }

                return insertResult == 1;
            }
        }

        /// <summary>
        /// Maps a query result set to an entity
        /// </summary>
        /// <param name="typeArr">Array with all sub-types</param>
        /// <param name="lookup">Dictionary to lookup entities</param>
        /// <param name="includes">Which children should be included</param>
        protected virtual Func<object[], T> MapDapperEntity<T>(Type[] typeArr, Dictionary<string, T> lookup, params Expression<Func<T, object>>[] includes)
            where T : class, new()
        {
            return (objectArr) => {
                var entityLookup        = objectArr[0] as T;
                var entityCompositeKey  = EntityMapper.GetCompositeUniqueKey<T>(entityLookup);
                var entityMap           = EntityMapper.GetEntityMap(typeof(T));
                T entity;

                if (!lookup.TryGetValue(entityCompositeKey, out entity))
                {
                    lookup.Add(entityCompositeKey, entity = entityLookup);
                }

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

        protected virtual async Task<InsertSqlQuery> InsertManyToOnes(object entity, InsertSqlQuery insertQuery)
        {
            var entityMap = EntityMapper.GetEntityMap(entity.GetType());

            var manyToOnes = entityMap.RelationProperties.Where(x => x.Key.GetCustomAttribute<ManyToOneAttribute>() != null);

            foreach (var one in manyToOnes)
            {
                var oneObj  = one.Key.GetValue(entity);
                var attr    = one.Key.GetCustomAttribute<ManyToOneAttribute>();

                if (oneObj != null)
                {
                    var oneObjKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", oneObj.GetType(), new[] { oneObj }) as string;
                    
                    // If it has no key, we can assume it is a new entity
                    if (oneObjKey == string.Empty || oneObjKey == null || new Guid(oneObjKey) == Guid.Empty)
                    {
                        // Insert it
                        var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", oneObj.GetType(), new[] { oneObj }, this.SqlGenerator) as InsertSqlQuery;
                        var queryResult = await this.ExecuteInsertQuery(oneObj, query);

                        // Grab primary key
                        oneObjKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", oneObj.GetType(), new[] { oneObj }) as string;

                        if (!queryResult)
                            throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                    }

                    insertQuery.Insert.Add(new InsertField(entityMap.TableName, attr.LocalKey, "@p_m2o_" + attr.TableName + "_" + attr.LocalKey));
                    insertQuery.Params.Add("@p_m2o_" + attr.TableName + "_" + attr.LocalKey, oneObjKey);
                }
            }

            return insertQuery;
        }

        protected virtual async Task<bool> InsertOneToManys(object entity, object foreignKey)
        {
            var entityMap = EntityMapper.GetEntityMap(entity.GetType());

            var oneToManys = entityMap.RelationProperties.Where(x => x.Key.GetCustomAttribute<OneToManyAttribute>() != null);

            foreach (var many in oneToManys)
            {
                var manyObj = many.Key.GetValue(entity) as IList;
                var attr    = many.Key.GetCustomAttribute<OneToManyAttribute>();
                var listEntityMap = EntityMapper.GetEntityMap(manyObj.GetType().GetGenericArguments()[0].GetTypeInfo());

                foreach (var obj in manyObj)
                {
                    var objKey  = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { obj }) as string;

                    // If it has no key, we can assume it is a new entity
                    if (objKey == string.Empty || objKey == null || new Guid(objKey) == Guid.Empty)
                    {
                        var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", listEntityMap.Type, new[] { obj }, this.SqlGenerator) as InsertSqlQuery;

                        query.Insert.Add(new InsertField(attr.TableName, attr.ExternalKey, "@p_fk_" + attr.ExternalKey));
                        query.Params.Add("@p_fk_" + attr.ExternalKey, foreignKey);

                        var queryResult = await this.ExecuteInsertQuery(obj, query);
                    }
                }
            }

            return true;
        }
    }
}
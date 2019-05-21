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

            // First grab & insert all the ManyToOnes (foreign keys of this entity)
            query = await this.InsertManyToOnes(entity, query);

            using (connection)
            {
                this.OpenConnection(connection);

                var insertResult = await connection.ExecuteAsync(query.ToString(), query.Params);

                if (insertResult == 1)
                {
                    var entityKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", entity.GetType(), new[] { entity }) as string;

                    // Insert the OneToManys
                    await this.InsertOneToManys(entity, entityKey);
                }

                return insertResult == 1;
            }
        }

        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="includes"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        public virtual async Task<bool> ExecuteUpdateQuery<T>(T entity, UpdateSqlQuery query, IDbConnection connection = null, params Expression<Func<T, object>>[] includes)
            where T : class, new()
        {
            if (connection == null)
                connection = this.DatabaseFactory.GetDatabaseConnection();

            // Update all children
            if (includes != null)
                query = await this.UpdateChildren<T>(entity, query, null, includes);

            using (connection)
            {
                this.OpenConnection(connection);

                var updateResult = await connection.ExecuteAsync(query.ToString(), query.Params);

                return updateResult == 1;
            }
        }

        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <returns>Number of deleted records</returns>
        public virtual Task<int> ExecuteDeleteQuery<T>(SqlQuery query, IDbConnection connection = null)
        {
            if (connection == null)
                connection = this.DatabaseFactory.GetDatabaseConnection();

            using (connection)
            {
                this.OpenConnection(connection);

                return connection.ExecuteAsync(query.ToString(), query.Params);
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

                foreach (var incl in includes)
                {
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
                }

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
                        // Insert
                        oneObjKey = await this.InsertEntityAndReturnId(oneObj) as string;

                        if (oneObjKey == null)
                            throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                    }

                    insertQuery.Insert.Add(new QueryField(entityMap.TableName, attr.LocalKey, "@p_m2o_" + attr.TableName + "_" + attr.LocalKey));
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

                if (manyObj == null) continue;

                var attr    = many.Key.GetCustomAttribute<OneToManyAttribute>();
                var listEntityMap = EntityMapper.GetEntityMap(manyObj.GetType().GetGenericArguments()[0].GetTypeInfo());

                foreach (var obj in manyObj)
                {
                    var objKey  = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { obj }) as string;

                    // If it has no key, we can assume it is a new entity
                    if (objKey == string.Empty || objKey == null || new Guid(objKey) == Guid.Empty)
                    {
                        var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", listEntityMap.Type, new[] { obj }, this.SqlGenerator) as InsertSqlQuery;

                        query.Insert.Add(new QueryField(attr.TableName, attr.ExternalKey, "@p_fk_" + attr.ExternalKey));
                        query.Params.Add("@p_fk_" + attr.ExternalKey, foreignKey);

                        var queryResult = await this.ExecuteInsertQuery(obj, query);
                    }
                }
            }

            return true;
        }

        protected virtual async Task<UpdateSqlQuery> UpdateChildren<T>(T entity, UpdateSqlQuery updateQuery, IDbConnection connection = null, params Expression<Func<T, object>>[] includes)
            where T : class, new()
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var foreignKey = EntityMapper.GetCompositeUniqueKey<T>(entity);

            foreach (var incl in includes)
            {
                var type = incl.Body.Type.GetTypeInfo();

                var exp = (MemberExpression)incl.Body;
                var property = entityMap.RelationProperties.Where(x => x.Key.Name == exp.Member.Name).SingleOrDefault();

                var oneObj   = property.Key.GetValue(entity);
                var attr     = property.Key.GetCustomAttribute<RelationAttributeBase>();

                if (oneObj != null)
                {
                    if (attr is ManyToOneAttribute)
                    {
                        var oneObjKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", oneObj.GetType(), new[] { oneObj }) as string;
                        
                        // If it has no key, we can assume it is a new entity
                        if (oneObjKey == string.Empty || oneObjKey == null || new Guid(oneObjKey) == Guid.Empty)
                        {
                            // Insert
                            oneObjKey = await this.InsertEntityAndReturnId(oneObj) as string;

                            if (oneObjKey == null)
                                throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                        }
                        else
                        {
                            // Update the entity
                            var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", oneObj.GetType(), new[] { oneObj }, this.SqlGenerator) as UpdateSqlQuery;
                            var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", oneObj.GetType(), new[] { oneObj, query, null, null }, this) as Task<bool>);

                            if (!queryResult)
                                throw new ApplicationException("Could not update a ManyToOne object: " + oneObj);
                        }

                        updateQuery.Updates.Add(new QueryField(entityMap.TableName, attr.LocalKey, "@p_m2o_" + attr.TableName + "_" + attr.LocalKey));
                        updateQuery.Params.Add("@p_m2o_" + attr.TableName + "_" + attr.LocalKey, oneObjKey);
                    }
                    else if (attr is OneToManyAttribute)
                    {
                        var currentChildrenIds = new List<object>();

                        var listObj = oneObj as IList;
                        var listType = listObj.GetType().GetGenericArguments()[0];
                        var listEntityMap = EntityMapper.GetEntityMap(listType);

                        foreach (var listItem in listObj)
                        {
                            var objKey  = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { listItem }) as string;

                            // If it has no key, we can assume it is a new entity
                            if (objKey == string.Empty || objKey == null || new Guid(objKey) == Guid.Empty)
                            {
                                var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", listEntityMap.Type, new[] { listItem }, this.SqlGenerator) as InsertSqlQuery;

                                query.Insert.Add(new QueryField(attr.TableName, attr.ExternalKey, "@p_fk_" + attr.ExternalKey));
                                query.Params.Add("@p_fk_" + attr.ExternalKey, foreignKey);

                                objKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { listItem }) as string;

                                var queryResult = await this.ExecuteInsertQuery(listItem, query);

                                if (!queryResult)
                                    throw new ApplicationException("Could not create a OneToMany object: " + listItem);
                            }
                            else
                            {
                                // Update the entity
                                var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", listType, new[] { listItem }, this.SqlGenerator) as UpdateSqlQuery;

                                query.Updates.Add(new QueryField(attr.TableName, attr.ExternalKey, "@p_fk_" + attr.ExternalKey));
                                query.Params.Add("@p_fk_" + attr.ExternalKey, foreignKey);

                                var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", listType, new[] { listItem, query, null, null }, this) as Task<bool>);

                                if (!queryResult)
                                    throw new ApplicationException("Could not update a OneToMany object: " + listItem);
                            }

                            Guid guidId;

                            if (Guid.TryParse(objKey, out guidId))
                                currentChildrenIds.Add(guidId);
                            else
                                currentChildrenIds.Add(objKey);
                        }

                        // Delete children not in list anymore
                        var deleteQuery = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "DeleteChildren", listType, new object[] { attr.TableName, foreignKey, attr.ExternalKey, attr.LocalKey, currentChildrenIds }, this.SqlGenerator) as SqlQuery;
                        
                        if (connection == null)
                            connection = this.DatabaseFactory.GetDatabaseConnection();

                        using (connection)
                        {
                            this.OpenConnection(connection);

                            await connection.QueryAsync(deleteQuery.ToString(), deleteQuery.Params);
                        }
                    }
                }
            }

            return updateQuery;
        }

        /// <summary>
        /// Inserts an entity and returns it composite ID
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>null when failed; id otherwise</returns>
        protected virtual async Task<object> InsertEntityAndReturnId(object entity)
        {
            // Insert it
            var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", entity.GetType(), new[] { entity }, this.SqlGenerator) as InsertSqlQuery;
            var queryResult = await this.ExecuteInsertQuery(entity, query);

            if (!queryResult)
                return null;

            // Grab primary key
            return ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", entity.GetType(), new[] { entity }) as string;
        }
    }
}
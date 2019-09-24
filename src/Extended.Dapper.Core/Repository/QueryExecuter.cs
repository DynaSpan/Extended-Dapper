using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Attributes.Entities.Relations;
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
        /// <param name="includes"></param>
        public virtual async Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var typeArr = ReflectionHelper.GetTypeListFromIncludes(includes).ToArray();
            
            // Grab keys
            var keys = query.Select.Where(x => x.IsMainKey).ToList();
            keys.Remove(keys.First()); // remove first key as it is from entity itself

            string splitOn = string.Join(",", keys.Select(k => k.Field));

            var entityLookup = new Dictionary<string, T>();

            var connection = this.DatabaseFactory.GetDatabaseConnection();

            try
            {
                this.OpenConnection(connection);

                await connection.QueryAsync<T>(query.ToString(), typeArr, this.MapDapperEntity(typeArr, entityLookup, includes), query.Params, null, true, splitOn);
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }
            finally
            {
                connection.Close();
            }

            return entityLookup.Values;
        }

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns>true when succesful; false otherwise</returns>
        public virtual async Task<bool> ExecuteInsertQuery(object entity, InsertSqlQuery query, IDbTransaction transaction = null)
        {
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
                // First grab & insert all the ManyToOnes (foreign keys of this entity)
                query = await this.InsertManyToOnes(entity, query, transaction);
                var insertResult = await transaction.Connection.ExecuteAsync(query.ToString(), query.Params, transaction);

                if (insertResult == 1)
                {
                    var entityKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", entity.GetType(), new[] { entity });

                    // Insert the OneToManys
                    await this.InsertOneToManys(entity, entityKey, transaction);
                }

                if (shouldCommit)
                    transaction.Commit();

                return insertResult == 1;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            finally 
            {
                if (connection != null)
                    connection.Close();
            }
        }

        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <param name="includes"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        public virtual async Task<bool> ExecuteUpdateQuery<T>(T entity, UpdateSqlQuery query, IDbTransaction transaction = null, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var shouldCommit = false;
            IDbConnection connection = null;

            if (transaction == null) 
            {
                connection = this.DatabaseFactory.GetDatabaseConnection();
                this.OpenConnection(connection);

                transaction = connection.BeginTransaction();
                shouldCommit = true;
            }

            // Update all children
            if (includes != null)
                query = await this.UpdateChildren<T>(entity, query, transaction, includes);

            try
            {
                var updateResult = await transaction.Connection.ExecuteAsync(query.ToString(), query.Params, transaction);

                if (shouldCommit)
                    transaction.Commit();

                return updateResult == 1;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                if (connection != null)
                    connection.Close();
            }
        }

        /// <summary>
        /// Executes a delete query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns>Number of deleted records</returns>
        public virtual async Task<int> ExecuteDeleteQuery<T>(SqlQuery query, IDbTransaction transaction = null)
        {
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
                var result = await transaction.Connection.ExecuteAsync(query.ToString(), query.Params, transaction);

                if (shouldCommit)
                    transaction.Commit();

                return result;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                if (connection != null)
                    connection.Close();
            }
        }

        /// <summary>
        /// Maps a query result set to an entity
        /// </summary>
        /// <param name="typeArr">Array with all sub-types</param>
        /// <param name="lookup">Dictionary to lookup entities</param>
        /// <param name="includes">Which children should be included</param>
        protected virtual Func<object[], T> MapDapperEntity<T>(Type[] typeArr, Dictionary<string, T> lookup, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            return (objectArr) => {
                var entityLookup        = objectArr[0] as T;
                var entityCompositeKey  = EntityMapper.GetCompositeUniqueKey<T>(entityLookup);
                var entityMap           = EntityMapper.GetEntityMap(typeof(T));
                T entity;

                if (!lookup.TryGetValue(entityCompositeKey.ToString(), out entity))
                {
                    lookup.Add(entityCompositeKey.ToString(), entity = entityLookup);
                }

                var singleObjectCacher = new Dictionary<Type, int>();

                foreach (var incl in includes)
                {
                    var exp = (MemberExpression)incl.Body;
                    var type = exp.Type.GetTypeInfo();

                    var property = entityMap.RelationProperties.Where(x => x.Key.Name == exp.Member.Name).SingleOrDefault();

                    if (type.IsGenericType)
                    {
                        // Handle as list, get all entities with this type
                        var listType     = type.GetGenericArguments()[0].GetTypeInfo();
                        var listProperty = property.Key.GetValue(entity) as IList;

                        //var defaultListObj = Activator.CreateInstance(listType);

                        var objList = objectArr.Where(x => x.GetType() == listType && /*!x.Equals(defaultListObj) &&*/ x != null).ToList();
                        IList value = ReflectionHelper.CastListTo(listType, objList);

                        if (value != null)
                        {
                            if (listProperty == null)
                                property.Key.SetValue(entity, value);
                            else 
                                foreach (var val in value)
                                    if (val != null && !listProperty.Contains(val))
                                        listProperty.Add(val);
                        }
                    }
                    else
                    {
                        object value;

                        // Handle as single object
                        if (singleObjectCacher.ContainsKey(type))
                        {
                            singleObjectCacher[type]++;
                            var index = singleObjectCacher[type];
                            var objArr = objectArr.Where(x => x.GetType() == type);

                            if (objArr.Count() > index)
                                value = objArr.ElementAt(index);
                            else
                                value = objArr.LastOrDefault();
                        }
                        else
                        {
                            value = objectArr.Where(x => x.GetType() == type).FirstOrDefault();
                            singleObjectCacher.Add(type, 0);
                        }

                        var valueId = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", new Type[] { type }, new [] { value });

                        if (property.Key != null && value != null && !EntityMapper.IsKeyEmpty(valueId))
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
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();

                if (connection.State != System.Data.ConnectionState.Open)
                    throw new ApplicationException("Could not connect to the SQL server");
            }
        }

        protected virtual async Task<InsertSqlQuery> InsertManyToOnes(object entity, InsertSqlQuery insertQuery, IDbTransaction transaction)
        {
            var entityMap = EntityMapper.GetEntityMap(entity.GetType());

            var manyToOnes = entityMap.RelationProperties.Where(x => x.Key.GetCustomAttribute<ManyToOneAttribute>() != null);

            foreach (var one in manyToOnes)
            {
                var oneObj  = one.Key.GetValue(entity);
                var attr    = one.Key.GetCustomAttribute<ManyToOneAttribute>();

                if (oneObj != null)
                {
                    var oneObjKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", oneObj.GetType(), new[] { oneObj });
                    
                    // If it has no key, we can assume it is a new entity
                    if (EntityMapper.IsKeyEmpty(oneObjKey))
                    {
                        // Insert
                        oneObjKey = await this.InsertEntityAndReturnId(oneObj, transaction);

                        if (oneObjKey == null)
                            throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                    }

                    insertQuery.Insert.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                    insertQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, oneObjKey);
                }
            }

            return insertQuery;
        }

        protected virtual async Task<bool> InsertOneToManys(object entity, object foreignKey, IDbTransaction transaction = null)
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
                    var objKey  = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { obj });

                    // If it has no key, we can assume it is a new entity
                    if (EntityMapper.IsKeyEmpty(objKey))
                    {
                        var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", listEntityMap.Type, new[] { obj }, this.SqlGenerator) as InsertSqlQuery;

                        query.Insert.Add(new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey));
                        query.Params.Add("p_fk_" + attr.ForeignKey, foreignKey);

                        var queryResult = await this.ExecuteInsertQuery(obj, query, transaction);
                    }
                }
            }

            return true;
        }

        protected virtual async Task<UpdateSqlQuery> UpdateChildren<T>(T entity, UpdateSqlQuery updateQuery, IDbTransaction transaction, params Expression<Func<T, object>>[] includes)
            where T : class
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
                        var oneObjKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", oneObj.GetType(), new[] { oneObj });
                        
                        // If it has no key, we can assume it is a new entity
                        if (EntityMapper.IsKeyEmpty(oneObjKey))
                        {
                            // Insert
                            oneObjKey = await this.InsertEntityAndReturnId(oneObj, transaction);

                            if (oneObjKey == null)
                                throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                        }
                        else
                        {
                            // Update the entity
                            var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", oneObj.GetType(), new[] { oneObj }, this.SqlGenerator) as UpdateSqlQuery;
                            var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", oneObj.GetType(), new[] { oneObj, query, transaction, null }, this) as Task<bool>);

                            if (!queryResult)
                                throw new ApplicationException("Could not update a ManyToOne object: " + oneObj);
                        }

                        updateQuery.Updates.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                        updateQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, oneObjKey);
                    }
                    else if (attr is OneToManyAttribute)
                    {
                        var currentChildrenIds = new List<object>();

                        var listObj = oneObj as IList;
                        var listType = listObj.GetType().GetGenericArguments()[0];
                        var listEntityMap = EntityMapper.GetEntityMap(listType);

                        foreach (var listItem in listObj)
                        {
                            var objKey  = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { listItem });

                            // If it has no key, we can assume it is a new entity
                            if (EntityMapper.IsKeyEmpty(objKey))
                            {
                                var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", listEntityMap.Type, new[] { listItem }, this.SqlGenerator) as InsertSqlQuery;

                                query.Insert.Add(new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey));
                                query.Params.Add("p_fk_" + attr.ForeignKey, foreignKey);

                                objKey = ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", listEntityMap.Type, new[] { listItem });

                                var queryResult = await this.ExecuteInsertQuery(listItem, query, transaction);

                                if (!queryResult)
                                    throw new ApplicationException("Could not create a OneToMany object: " + listItem);
                            }
                            else
                            {
                                // Update the entity
                                var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", listType, new[] { listItem }, this.SqlGenerator) as UpdateSqlQuery;

                                query.Updates.Add(new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey));
                                query.Params.Add("p_fk_" + attr.ForeignKey, foreignKey);

                                var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", listType, new[] { listItem, query, transaction, null }, this) as Task<bool>);

                                if (!queryResult)
                                    throw new ApplicationException("Could not update a OneToMany object: " + listItem);
                            }

                            Guid guidId;

                            if (Guid.TryParse(objKey.ToString(), out guidId))
                                currentChildrenIds.Add(guidId);
                            else
                                currentChildrenIds.Add(objKey);
                        }

                        // Delete children not in list anymore
                        var deleteQuery = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "DeleteChildren", listType, new object[] { attr.TableName, foreignKey, attr.ForeignKey, attr.LocalKey, currentChildrenIds }, this.SqlGenerator) as SqlQuery;
                        
                        try
                        {
                            await transaction.Connection.QueryAsync(deleteQuery.ToString(), deleteQuery.Params, transaction);
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
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
        /// <param name="transaction"></param>
        /// <returns>null when failed; id otherwise</returns>
        protected virtual async Task<object> InsertEntityAndReturnId(object entity, IDbTransaction transaction = null)
        {
            // Insert it
            var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Insert", entity.GetType(), new[] { entity }, this.SqlGenerator) as InsertSqlQuery;
            var queryResult = await this.ExecuteInsertQuery(entity, query, transaction);

            if (!queryResult)
                return null;

            // Grab primary key
            return ReflectionHelper.CallGenericMethod(typeof(EntityMapper), "GetCompositeUniqueKey", entity.GetType(), new[] { entity });
        }
    }
}
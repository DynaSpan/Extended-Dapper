using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.Metadata;
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
        public virtual async Task<IEnumerable<T>> ExecuteSelectQuery<T>(SelectSqlQuery query, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            var typeArr = ReflectionHelper.GetTypeListFromIncludes<T>(includes).ToArray();
            
            // Grab keys
            var keys = query.Select.Where(x => x.IsMainKey).ToList();
            keys.Remove(keys.First()); // remove first key as it is from entity itself

            string splitOn = string.Join(",", keys.Select(k => k.Field));

            var entityLookup = new Dictionary<string, T>();

            var connection = this.DatabaseFactory.GetDatabaseConnection();

            try
            {
                this.OpenConnection(connection);

                await connection.QueryAsync<T>(query.ToString(), typeArr, DapperMapper.MapDapperEntity<T>(typeArr, entityLookup, includes), query.Params, null, true, splitOn);
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
        /// <param name="transaction"></param>
        /// <returns>true when succesful; false otherwise</returns>
        public virtual Task<bool> ExecuteInsertEntityQuery<T>(T entity, IDbTransaction transaction = null, Type typeOverride = null)
            where T : class
            => this.ExecuteInsertQuery<T>(entity, transaction);

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="query"></paran>
        /// <param name="transaction"></param>
        /// <param name="queryFields"></param>
        /// <param name="queryParams"></param>
        /// <returns>true when succesful; false otherwise</returns>
        public virtual async Task<bool> ExecuteInsertQuery<T>(
            T entity, 
            IDbTransaction transaction = null, 
            Type typeOverride = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null)
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

            try
            {
                var entityKey = EntityMapper.GetCompositeUniqueKey<T>(entity, typeOverride);
                bool hasNoKey = EntityMapper.IsKeyEmpty(entityKey);

                // First grab & insert all the ManyToOnes (foreign keys of this entity)
                var m2oInsertQuery = await this.InsertManyToOnes<T>(entity, transaction, typeOverride, queryFields, queryParams);

                hasNoKey = hasNoKey && EntityMapper.IsKeyEmpty(entityKey);

                int insertResult = 0;

                if (hasNoKey)
                {
                    InsertSqlQuery insertQuery = this.SqlGenerator.Insert<T>(entity, typeOverride);
                    entityKey = EntityMapper.GetCompositeUniqueKey<T>(entity, typeOverride);

                    if (queryFields != null)
                        insertQuery.Insert.AddRange(queryFields);

                    if (queryParams != null)
                        foreach (var param in queryParams)
                            insertQuery.Params.Add(param.Key, param.Value);
            
                    insertQuery.Insert.AddRange(m2oInsertQuery.Insert);

                    foreach (var param in m2oInsertQuery.Params)
                        if (!insertQuery.Params.ContainsKey(param.Key))
                            insertQuery.Params.Add(param.Key, param.Value);
                
                    insertResult = await transaction.Connection.ExecuteAsync(insertQuery.ToString(), insertQuery.Params, transaction);
                }

                if (insertResult == 1 || !hasNoKey)
                {
                    // Insert the OneToManys
                    if (!await this.InsertOneToManys<T>(entity, entityKey, transaction, typeOverride))
                        insertResult = -1;
                }

                if (shouldCommit)
                    transaction.Commit();

                connection?.Close();

                return insertResult == 1 || (insertResult == 0 && !hasNoKey);
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

        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <param name="includes"></param>
        /// <param name="queryFields"></param>
        /// <param name="queryParams"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        public virtual async Task<bool> ExecuteUpdateQuery<T>(
            T entity, 
            IDbTransaction transaction = null, 
            Expression<Func<T, object>>[] includes = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null)
            where T : class
        {
            var query = this.SqlGenerator.Update<T>(entity);
            var shouldCommit = false;
            IDbConnection connection = null;

            if (transaction == null) 
            {
                connection = this.DatabaseFactory.GetDatabaseConnection();
                this.OpenConnection(connection);

                transaction = connection.BeginTransaction();
                shouldCommit = true;
            }

            if (queryFields != null)
                query.Updates.AddRange(queryFields);

            if (queryParams != null)
                foreach (var param in queryParams)
                    query.Params.Add(param.Key, param.Value);

            // Update all children
            if (includes != null)
            {
                var updateQuery = await this.UpdateChildren<T>(entity, transaction, includes);

                updateQuery.Updates.AddRange(updateQuery.Updates);

                foreach (var param in updateQuery.Params)
                    if (!updateQuery.Params.ContainsKey(param.Key))
                        updateQuery.Params.Add(param.Key, param.Value);
            }

            try
            {
                var updateResult = await transaction.Connection.ExecuteAsync(query.ToString(), query.Params, transaction);

                if (shouldCommit)
                    transaction.Commit();

                connection?.Close();

                return updateResult == 1;
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
                var result = await transaction.Connection.ExecuteAsync(query.ToString(), query.Params, transaction);

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

        protected virtual async Task<InsertSqlQuery> InsertManyToOnes<T>(
            T entity, 
            IDbTransaction transaction, 
            Type typeOverride = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null)
            where T : class
        {
            EntityMap entityMap;

            if (typeOverride != null)
                entityMap = EntityMapper.GetEntityMap(typeOverride);
            else
                entityMap = EntityMapper.GetEntityMap(typeof(T));

            var manyToOnes = entityMap.RelationProperties.Where(x => x.Key.GetCustomAttributes<ManyToOneAttribute>().Any());

            InsertSqlQuery insertQuery = new InsertSqlQuery();
            insertQuery.Insert = new List<QueryField>();
            insertQuery.Params = new Dictionary<string, object>();

            foreach (var one in manyToOnes)
            {
                var oneObj          = one.Key.GetValue(entity);
                var attr            = one.Key.GetCustomAttribute<ManyToOneAttribute>();

                if (oneObj != null)
                {
                    Type oneObjType = oneObj.GetType();
                    var oneObjKey = EntityMapper.GetCompositeUniqueKey(oneObj, oneObjType);
                    
                    // If it has no key, we can assume it is a new entity
                    if (EntityMapper.IsKeyEmpty(oneObjKey))
                    {
                        // Insert
                        oneObjKey = await this.InsertEntityAndReturnId(oneObj, oneObjType, transaction);

                        if (oneObjKey == null)
                            throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                    }

                    if (queryParams == null || !queryParams.ContainsKey("p_fk_" + attr.ForeignKey))
                    {
                        insertQuery.Insert.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                        insertQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, oneObjKey);
                    }
                } 
                else if (!attr.Nullable && (queryParams == null || !queryParams.ContainsKey("p_fk_" + attr.ForeignKey)))
                {
                    var oneObjEntityMap = EntityMapper.GetEntityMap(one.Key.PropertyType);
                    var prop = oneObjEntityMap.RelationProperties.Where(p => p.Key.GetCustomAttribute<OneToManyAttribute>() != null
                        && p.Key.GetCustomAttribute<OneToManyAttribute>().ForeignKey == attr.ForeignKey).Count();

                    if (prop > 0)
                    {
                        var entityKey = EntityMapper.GetCompositeUniqueKey<T>(entity);

                        insertQuery.Insert.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                        insertQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, entityKey);
                    }
                }
            }

            return insertQuery;
        }

        protected virtual async Task<bool> InsertOneToManys<T>(T entity, object foreignKey, IDbTransaction transaction = null, Type typeOverride = null)
            where T : class
        {
            EntityMap entityMap;

            if (typeOverride != null)
                entityMap = EntityMapper.GetEntityMap(typeOverride);
            else
                entityMap = EntityMapper.GetEntityMap(typeof(T));

            var oneToManys = entityMap.RelationProperties.Where(x => x.Key.GetCustomAttribute<OneToManyAttribute>() != null);

            foreach (var many in oneToManys)
            {
                var manyObj = many.Key.GetValue(entity) as IList;

                if (manyObj == null) 
                    continue;

                var attr            = many.Key.GetCustomAttribute<OneToManyAttribute>();
                var listType        = manyObj.GetType().GetGenericArguments()[0].GetTypeInfo();
                var listEntityMap   = EntityMapper.GetEntityMap(listType);

                foreach (var obj in manyObj)
                {
                    Type objType = obj.GetType();
                    var objKey = EntityMapper.GetCompositeUniqueKey(obj, objType);

                    // If it has no key, we can assume it is a new entity
                    if (EntityMapper.IsKeyEmpty(objKey))
                    {
                        var queryField = new List<QueryField>();
                        queryField.Add(new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey));
                        
                        var queryParams = new Dictionary<string, object>();
                        queryParams.Add("p_fk_" + attr.ForeignKey, foreignKey);

                        var queryResult = await this.ExecuteInsertQuery(obj, transaction, objType, queryField, queryParams);

                        if (!queryResult)
                            return false;
                    }
                }
            }

            return true;
        }

        protected virtual async Task<UpdateSqlQuery> UpdateChildren<T>(T entity, IDbTransaction transaction, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            UpdateSqlQuery updateQuery = new UpdateSqlQuery();
            updateQuery.Updates = new List<QueryField>();
            updateQuery.Params = new Dictionary<string, object>();

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
                        var objType = oneObj.GetType();
                        var oneObjKey = EntityMapper.GetCompositeUniqueKey(oneObj, objType);
                        
                        // If it has no key, we can assume it is a new entity
                        if (EntityMapper.IsKeyEmpty(oneObjKey))
                        {
                            // Insert
                            oneObjKey = await this.InsertEntityAndReturnId(oneObj, objType, transaction);

                            if (oneObjKey == null)
                                throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                        }
                        else
                        {
                            // Update the entity
                            var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", objType, new[] { oneObj }, this.SqlGenerator) as UpdateSqlQuery;
                            var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", oneObj.GetType(), new[] { oneObj, transaction, null, null }, this) as Task<bool>);

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
                            var objKey = EntityMapper.GetCompositeUniqueKey(listItem, listType);

                            // If it has no key, we can assume it is a new entity
                            if (EntityMapper.IsKeyEmpty(objKey))
                            {
                                var queryField = new List<QueryField>();
                                queryField.Add(new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey));
                                
                                var queryParams = new Dictionary<string, object>();
                                queryParams.Add("p_fk_" + attr.ForeignKey, foreignKey);

                                var queryResult = await this.ExecuteInsertQuery(listItem, transaction, listType, queryField, queryParams);

                                objKey = EntityMapper.GetCompositeUniqueKey(listItem, listType);

                                if (!queryResult)
                                    throw new ApplicationException("Could not create a OneToMany object: " + listItem);
                            }
                            else
                            {
                                // Update the entity
                                var queryField = new List<QueryField>();
                                queryField.Add(new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey));
                                
                                var queryParams = new Dictionary<string, object>();
                                queryParams.Add("p_fk_" + attr.ForeignKey, foreignKey);

                                var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", listType, new[] { listItem, transaction, null, queryField, queryParams }, this) as Task<bool>);

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
                            try {
                                transaction?.Rollback();
                            } catch (Exception) { }

                            transaction.Connection?.Close();

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
        protected virtual async Task<object> InsertEntityAndReturnId<T>(T entity, Type typeOverride = null, IDbTransaction transaction = null)
            where T : class
        {
            // Insert it
            var queryResult = await this.ExecuteInsertQuery(entity, transaction, typeOverride);

            if (!queryResult)
                return null;

            // Grab primary key
            return EntityMapper.GetCompositeUniqueKey<T>(entity, typeOverride);
        }
    }
}
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
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Models;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql.Generator;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.QueryExecuter
{
    public partial class QueryExecuter : IQueryExecuter
    {
        /// <summary>
        /// Executes an update query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <param name="updateFields"></param>
        /// <param name="includes"></param>
        /// <param name="queryFields"></param>
        /// <param name="queryParams"></param>
        /// <param name="typeOverride"></param>
        /// <returns>True when succesfull; false otherwise</returns>
        public virtual async Task<bool> ExecuteUpdateQuery<T>(
            T entity,
            IDbTransaction transaction = null,
            Expression<Func<T, object>>[] updateFields = null,
            Expression<Func<T, object>>[] includes = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null,
            Type typeOverride = null)
            where T : class
        {
            UpdateSqlQuery query;
            EntityMap entityMap;

            if (typeOverride == null)
            {
                query = this.SqlGenerator.Update<T>(entity, updateFields);
                entityMap = EntityMapper.GetEntityMap(typeof(T));
            } else {
                query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", typeOverride, new object[] { entity, updateFields }, this.SqlGenerator) as UpdateSqlQuery;
                entityMap = EntityMapper.GetEntityMap(typeOverride);
            }

            IEnumerable<EntityKey> foreignKeys;

            // Check if keys are filled
            if (EntityMapper.IsAutovalueKeysEmpty(entity, typeOverride) && EntityMapper.IsAlternativeKeysEmpty(entity, typeOverride))
            {
                throw new NotSupportedException("Could not update entity based on key, as no keys have been provided");
            }
            else if (EntityMapper.IsAutovalueKeysEmpty(entity, typeOverride) && !EntityMapper.IsAlternativeKeysEmpty(entity, typeOverride) && includes != null)
            {
                // Grab proper entity keys
                foreignKeys = await this.GetEntityKeysFromAlternativeKeys(entity, typeOverride ?? typeof(T), transaction).ConfigureAwait(false);
            }
            else
            {
                foreignKeys = EntityMapper.GetEntityKeys(entity, typeOverride);
            }

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
            {
                foreach (var param in queryParams)
                {
                    query.Params.Add(param.Key, param.Value);
                }
            }

            bool executeChildrenUpdates = true;

            if (includes == null && updateFields != null)
            {
                executeChildrenUpdates = false;
                includes = updateFields.Where(f => entityMap.RelationProperties.Any(r => r.Key.Name == ExpressionHelper.GetMemberExpression(f).Member.Name)).ToArray();
            }

            // Update all children
            if (includes != null)
            {
                UpdateSqlQuery updateQuery;

                if (typeOverride == null)
                    updateQuery = await this.UpdateChildren<T>(entity, transaction, typeOverride, foreignKeys, executeChildrenUpdates, includes).ConfigureAwait(false);
                else
                    updateQuery = await this.UpdateChildren(entity, transaction, typeOverride, foreignKeys, executeChildrenUpdates, includes).ConfigureAwait(false);

                query.Updates.AddRange(updateQuery.Updates);

                foreach (var param in updateQuery.Params)
                {
                    if (!query.Params.ContainsKey(param.Key))
                    {
                        query.Params.Add(param.Key, param.Value);
                    }
                }
            }

            try
            {
                string updateQuery = this.DatabaseFactory.SqlProvider.BuildUpdateQuery(query);
                var updateResult = await transaction.Connection.ExecuteAsync(updateQuery, query.Params, transaction).ConfigureAwait(false);

                if (shouldCommit)
                    transaction.Commit();

                connection?.Close();

                return updateResult > 0;
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

        protected virtual async Task<UpdateSqlQuery> UpdateChildren<T>(T entity, IDbTransaction transaction, Type typeOverride = null, IEnumerable<EntityKey> foreignKeysParam = null, bool executeUpdates = true, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            UpdateSqlQuery updateQuery = new UpdateSqlQuery
            {
                Updates = new List<QueryField>(),
                Params = new Dictionary<string, object>()
            };

            EntityMap entityMap = EntityMapper.GetEntityMap(typeOverride ?? typeof(T));
            IEnumerable<EntityKey> foreignKey = foreignKeysParam ?? EntityMapper.GetEntityKeys<T>(entity, typeOverride);

            foreach (var incl in includes)
            {
                var type = incl.Body.Type.GetTypeInfo();

                var exp = (MemberExpression)incl.Body;
                var property = entityMap.RelationProperties.SingleOrDefault(x => string.Equals(x.Key.Name, exp.Member.Name, StringComparison.InvariantCultureIgnoreCase));

                var oneObj   = property.Key.GetValue(entity);
                var attr     = property.Key.GetCustomAttribute<RelationAttributeBase>();

                EntityMap inclEntityMap;

                if (oneObj != null)
                {
                    if (attr is ManyToOneAttribute)
                    {
                        var objType = oneObj.GetType();
                        var oneObjKey = EntityMapper.GetEntityKeys(oneObj, objType).SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));
                        inclEntityMap = EntityMapper.GetEntityMap(objType);

                        // If it has no key, we can assume it is a new entity
                        if (EntityMapper.IsAutovalueKeysEmpty(oneObj, objType) && EntityMapper.IsAlternativeKeysEmpty(oneObj, objType))
                        {
                            // Insert
                            oneObjKey = (await this.InsertEntityAndReturnId(oneObj, objType, transaction).ConfigureAwait(false)).SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                            if (oneObjKey == null)
                                throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                        }
                        else
                        {
                            // Update the entity
                            if (executeUpdates)
                            {
                                var query = ReflectionHelper.CallGenericMethod(typeof(SqlGenerator), "Update", objType, new[] { oneObj, null }, this.SqlGenerator) as UpdateSqlQuery;
                                var queryResult = await this.ExecuteUpdateQuery(oneObj, transaction, null, null, null, null, objType).ConfigureAwait(false);

                                if (!queryResult)
                                    throw new ApplicationException("Could not update a ManyToOne object: " + oneObj);
                            }

                            if (EntityMapper.IsAutovalueKeysEmpty(oneObj, objType) && !EntityMapper.IsAlternativeKeysEmpty(oneObj, objType))
                            {
                                // Entity exists already but does not have primary key
                                // so we load the entity
                                var oneObjKeys = await this.GetEntityKeysFromAlternativeKeys(oneObj, objType, transaction).ConfigureAwait(false);

                                if (oneObjKeys != null)
                                    oneObjKey = oneObjKeys.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));
                            }
                        }

                        updateQuery.Updates.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                        updateQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, oneObjKey.Value);
                    }
                    else if (attr is OneToManyAttribute)
                    {
                        bool useAutoValueKey = false;
                        string autoValueKeyName = null;
                        var currentChildrenIds = new List<object>();

                        var listObj = oneObj as IList;

                        var listType = listObj.GetListType();
                        inclEntityMap = EntityMapper.GetEntityMap(listType);

                        foreach (var listItem in listObj)
                        {
                            var objKey = EntityMapper.GetEntityKeys(listItem, listType).SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                            // If it has no key, we can assume it is a new entity
                            if (EntityMapper.IsAutovalueKeysEmpty(listItem, listType) && EntityMapper.IsAlternativeKeysEmpty(listItem, listType))
                            {
                                var queryField = new List<QueryField>()
                                {
                                    new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey)
                                };

                                var queryParams = new Dictionary<string, object>()
                                {
                                    { "p_fk_" + attr.ForeignKey, foreignKey.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase)).Value }
                                };

                                var queryResult = await this.ExecuteInsertQuery(listItem, transaction, listType, false, queryField, queryParams).ConfigureAwait(false);

                                if (!queryResult)
                                    throw new ApplicationException("Could not create a OneToMany object: " + listItem);

                                var entityKeys = EntityMapper.GetEntityKeys(listItem, listType);
                                objKey = entityKeys.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                                // Specific use-case, for when an object doesn't follow standard
                                // SQL conventions with naming local keys (e.g. different key names in different classes)
                                if (objKey == null) {
                                    objKey = entityKeys.FirstOrDefault(k => k.AutoValue);
                                    useAutoValueKey = true;
                                    autoValueKeyName = objKey.Name;
                                }
                            }
                            else
                            {
                                // Update the entity
                                if (executeUpdates)
                                {
                                    var queryField = new List<QueryField>()
                                    {
                                        new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey)
                                    };

                                    var queryParams = new Dictionary<string, object>()
                                    {
                                        { "p_fk_" + attr.ForeignKey, foreignKey.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase)).Value }
                                    };

                                    var queryResult = await this.ExecuteUpdateQuery(listItem, transaction, null, null, queryField, queryParams, listType).ConfigureAwait(false);

                                    if (!queryResult)
                                        throw new ApplicationException("Could not update a OneToMany object: " + listItem);
                                }

                                objKey = EntityMapper.GetEntityKeys(listItem, listType).SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                                // Grab by alternative key
                                if (EntityMapper.IsAutovalueKeysEmpty(listItem, listType) && !EntityMapper.IsAlternativeKeysEmpty(listItem, listType))
                                {
                                    var altEntityKeys = await this.GetEntityKeysFromAlternativeKeys(listItem, listType, transaction).ConfigureAwait(false);

                                    if (altEntityKeys != null)
                                        objKey = altEntityKeys.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));
                                }
                            }

                            // Specific use-case, for when an object doesn't follow standard
                            // SQL conventions with naming local keys (e.g. different key names in different classes)
                            if (objKey == null) {
                                var entityKeys = EntityMapper.GetEntityKeys(listItem, listType);
                                objKey = entityKeys.FirstOrDefault(k => k.AutoValue);
                                useAutoValueKey = true;
                                autoValueKeyName = objKey.Name;
                            }

                            if (Guid.TryParse(objKey?.Value?.ToString(), out Guid guidId))
                                currentChildrenIds.Add(guidId);
                            else
                                currentChildrenIds.Add(objKey.Value);
                        }

                        string localKey = attr.LocalKey;

                        if (useAutoValueKey)
                            localKey = autoValueKeyName;

                        // Delete children not in list anymore
                        var deleteQuery = this.SqlGenerator.DeleteChildren<object>(attr.TableName, foreignKey.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase)).Value, attr.ForeignKey, localKey, currentChildrenIds, listType);

                        try
                        {
                            string query = this.DatabaseFactory.SqlProvider.BuildDeleteQuery(deleteQuery, inclEntityMap);
                            await transaction.Connection.QueryAsync(query, deleteQuery.Params, transaction).ConfigureAwait(false);
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
    }
}
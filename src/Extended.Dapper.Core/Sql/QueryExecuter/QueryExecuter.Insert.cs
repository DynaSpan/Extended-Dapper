using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Models;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Core.Sql.QueryExecuter
{
    public partial class QueryExecuter : IQueryExecuter
    {
        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <param name="typeOverride"></param>
        /// <param name="forceInsert"></param>
        /// <returns>true when succesful; false otherwise</returns>
        public virtual Task<bool> ExecuteInsertEntityQuery<T>(T entity, IDbTransaction transaction = null, Type typeOverride = null, bool forceInsert = false)
            where T : class
            => this.ExecuteInsertQuery<T>(entity, transaction, typeOverride, forceInsert);

        /// <summary>
        /// Executes an insert query
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <param name="typeOverride"></param>
        /// <param name="forceInsert"></param>
        /// <param name="queryFields"></param>
        /// <param name="queryParams"></param>
        /// <returns>true when succesful; false otherwise</returns>
        public virtual async Task<bool> ExecuteInsertQuery<T>(
            T entity,
            IDbTransaction transaction = null,
            Type typeOverride = null,
            bool forceInsert = false,
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
                var entityKey = EntityMapper.GetEntityKeys<T>(entity, typeOverride);
                bool hasNoKey = EntityMapper.IsAutovalueKeysEmpty<T>(entity, typeOverride) && EntityMapper.IsAlternativeKeysEmpty<T>(entity, typeOverride);

                // First grab & insert all the ManyToOnes (foreign keys of this entity)
                var m2oInsertQuery = await this.InsertManyToOnes<T>(entity, transaction, typeOverride, queryFields, queryParams).ConfigureAwait(false);

                hasNoKey = hasNoKey && EntityMapper.IsAutovalueKeysEmpty<T>(entity, typeOverride) && EntityMapper.IsAlternativeKeysEmpty<T>(entity, typeOverride);

                int insertResult = 0;

                if (hasNoKey || forceInsert)
                {
                    InsertSqlQuery insertQuery = this.SqlGenerator.Insert<T>(entity, typeOverride);
                    entityKey = EntityMapper.GetEntityKeys<T>(entity, typeOverride);

                    if (queryFields != null)
                        insertQuery.Insert.AddRange(queryFields);

                    if (queryParams != null)
                    {
                        foreach (var param in queryParams)
                        {
                            insertQuery.Params.Add(param.Key, param.Value);
                        }
                    }

                    insertQuery.Insert.AddRange(m2oInsertQuery.Insert);

                    foreach (var param in m2oInsertQuery.Params)
                    {
                        if (!insertQuery.Params.ContainsKey(param.Key))
                        {
                            insertQuery.Params.Add(param.Key, param.Value);
                        }
                    }

                    string query = this.DatabaseFactory.SqlProvider.BuildInsertQuery(insertQuery);

                    if (insertQuery.AutoIncrementKey)
                    {
                        var insertedKey = await transaction.Connection.QuerySingleAsync<int>(query, insertQuery.Params, transaction).ConfigureAwait(false);

                        if ((int)insertedKey != default)
                            insertResult = 1;

                        insertQuery.AutoIncrementField.PropertyInfo.SetValue(entity, insertedKey);

                        if (entityKey.Count() == 1)
                            entityKey.First().Value = insertedKey;
                        else
                            entityKey.Single(k => k.Property.Name == insertQuery.AutoIncrementField.PropertyName).Value = insertedKey;
                    }
                    else
                    {
                        insertResult = await transaction.Connection.ExecuteAsync(query, insertQuery.Params, transaction).ConfigureAwait(false);
                    }
                }

                if (insertResult > 0 || !hasNoKey)
                {
                    // Insert the OneToManys
                    if (!await this.InsertOneToManys<T>(entity, entityKey.First().Value, transaction, typeOverride).ConfigureAwait(false))
                        insertResult = -1;
                }

                if (shouldCommit)
                    transaction.Commit();

                connection?.Close();

                return insertResult > 0 || (insertResult == 0 && !hasNoKey);
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
        /// Inserts an entity and returns it composite ID
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns>null when failed; id otherwise</returns>
        protected virtual async Task<IEnumerable<EntityKey>> InsertEntityAndReturnId<T>(T entity, Type typeOverride = null, IDbTransaction transaction = null)
            where T : class
        {
            // Insert it
            var queryResult = await this.ExecuteInsertQuery(entity, transaction, typeOverride).ConfigureAwait(false);

            if (!queryResult)
                return null;

            // Grab primary key
            return EntityMapper.GetEntityKeys<T>(entity, typeOverride);
        }

        protected virtual async Task<InsertSqlQuery> InsertManyToOnes<T>(
            T entity,
            IDbTransaction transaction,
            Type typeOverride = null,
            IEnumerable<QueryField> queryFields = null,
            Dictionary<string, object> queryParams = null)
            where T : class
        {
            EntityMap entityMap = EntityMapper.GetEntityMap(typeOverride ?? typeof(T));

            var manyToOnes = entityMap.RelationPropertiesMetadata.Where(x => x.RelationPropertyInfo.GetCustomAttributes<ManyToOneAttribute>().Any());

            InsertSqlQuery insertQuery = new InsertSqlQuery
            {
                Insert = new List<QueryField>(),
                Params = new Dictionary<string, object>()
            };

            foreach (var one in manyToOnes)
            {
                var oneObj          = one.PropertyInfo.GetValue(entity);
                var attr            = one.PropertyInfo.GetCustomAttribute<ManyToOneAttribute>();

                if (oneObj != null)
                {
                    Type oneObjType = oneObj.GetType();
                    var oneObjKey = EntityMapper.GetEntityKeys(oneObj, oneObjType).Single(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                    // If it has no key, we can assume it is a new entity
                    if (EntityMapper.IsAutovalueKeysEmpty(oneObj, oneObjType) && !EntityMapper.IsAlternativeKeysEmpty(oneObj, oneObjType)
                        && (queryParams?.ContainsKey("p_fk_" + attr.ForeignKey) != true))
                    {
                        // Entity exists already but does not have primary key
                        // so we load the entity
                        var oneObjKeys = await this.GetEntityKeysFromAlternativeKeys(oneObj, oneObjType, transaction).ConfigureAwait(false);

                        if (oneObjKeys != null)
                            oneObjKey = oneObjKeys.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));
                    }
                    else if (EntityMapper.IsAutovalueKeysEmpty(oneObj, oneObjType))
                    {
                        // Insert
                        oneObjKey = (await this.InsertEntityAndReturnId(oneObj, oneObjType, transaction).ConfigureAwait(false)).SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                        if (oneObjKey == null)
                            throw new ApplicationException("Could not insert a ManyToOne object: " + oneObj);
                    }

                    if (queryParams?.ContainsKey("p_fk_" + attr.ForeignKey) != true)
                    {
                        insertQuery.Insert.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                        insertQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, oneObjKey.Value);
                    }
                }
                else if (!attr.Nullable && (queryParams?.ContainsKey("p_fk_" + attr.ForeignKey) != true))
                {
                    var oneObjType = one.PropertyInfo.PropertyType;
                    var oneObjEntityMap = EntityMapper.GetEntityMap(oneObjType);
                    var prop = oneObjEntityMap.RelationProperties.Count(p => p.Key.GetCustomAttribute<OneToManyAttribute>() != null
                        && string.Equals(p.Key.GetCustomAttribute<OneToManyAttribute>().ForeignKey, attr.ForeignKey, StringComparison.InvariantCultureIgnoreCase));

                    if (prop > 0)
                    {
                        var entityKey = EntityMapper.GetEntityKeys<T>(entity).SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));

                        if (EntityMapper.IsAutovalueKeysEmpty(oneObj, oneObjType) && !EntityMapper.IsAlternativeKeysEmpty(oneObj, oneObjType)
                            && (queryParams?.ContainsKey("p_fk_" + attr.ForeignKey) != true))
                        {
                            // Entity exists already but does not have primary key
                            // so we load the entity
                            var oneObjKeys = await this.GetEntityKeysFromAlternativeKeys(oneObj, oneObjType, transaction).ConfigureAwait(false);

                            if (oneObjKeys != null)
                                entityKey = oneObjKeys.SingleOrDefault(k => string.Equals(k.Name, attr.LocalKey, StringComparison.InvariantCultureIgnoreCase));
                        }

                        insertQuery.Insert.Add(new QueryField(entityMap.TableName, attr.ForeignKey, "p_m2o_" + attr.TableName + "_" + attr.ForeignKey));
                        insertQuery.Params.Add("p_m2o_" + attr.TableName + "_" + attr.ForeignKey, entityKey?.Value);
                    }
                }
            }

            return insertQuery;
        }

        protected virtual async Task<bool> InsertOneToManys<T>(T entity, object foreignKey, IDbTransaction transaction = null, Type typeOverride = null)
            where T : class
        {
            var entityMap = EntityMapper.GetEntityMap(typeOverride ?? typeof(T));

            foreach (var many in entityMap.RelationProperties.Where(x => x.Key.GetCustomAttribute<OneToManyAttribute>() != null))
            {
                if (!(many.Key.GetValue(entity) is IList manyObj))
                    continue;

                var attr            = many.Key.GetCustomAttribute<OneToManyAttribute>();
                var listType        = manyObj.GetListType().GetTypeInfo();
                var listEntityMap   = EntityMapper.GetEntityMap(listType);

                foreach (var obj in manyObj)
                {
                    Type objType = obj.GetType();

                    // If it has no key, we can assume it is a new entity
                    if (EntityMapper.IsAutovalueKeysEmpty(obj, objType) && EntityMapper.IsAlternativeKeysEmpty(obj, objType))
                    {
                        var queryField = new List<QueryField>
                        {
                            new QueryField(attr.TableName, attr.ForeignKey, "p_fk_" + attr.ForeignKey)
                        };

                        var queryParams = new Dictionary<string, object>
                        {
                            { "p_fk_" + attr.ForeignKey, foreignKey }
                        };

                        var queryResult = await this.ExecuteInsertQuery(obj, transaction, objType, false, queryField, queryParams).ConfigureAwait(false);

                        if (!queryResult)
                            return false;
                    }
                }

                many.Key.SetValue(entity, manyObj);
            }

            return true;
        }
    }
}
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
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql.Generator;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Models;

namespace Extended.Dapper.Sql.QueryExecuter
{
    public partial class QueryExecuter : IQueryExecuter
    {
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
                            var queryResult = await (ReflectionHelper.CallGenericMethod(typeof(QueryExecuter), "ExecuteUpdateQuery", oneObj.GetType(), new[] { oneObj, transaction, null, null, null }, this) as Task<bool>);

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

                                var queryResult = await this.ExecuteInsertQuery(listItem, transaction, listType, false, queryField, queryParams);

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
    }
}
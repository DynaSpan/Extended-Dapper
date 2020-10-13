using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Models;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql.Generator
{
    public partial class SqlGenerator : ISqlGenerator
    {
        private readonly ISqlQueryProvider sqlProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseProvider">Which database we connect to; defaults to MSSQL</param>
        public SqlGenerator(DatabaseProvider databaseProvider = DatabaseProvider.MSSQL)
        {
            this.sqlProvider = SqlQueryProviderHelper.GetProvider(databaseProvider);

            // Check if it is implemented
            if (this.sqlProvider == null)
                throw new ArgumentException(databaseProvider.ToString() + " is currently not implemented");
        }

        /// <summary>
        /// Creates an search expression for the ID
        /// </summary>
        /// <param name="id">The id that is wanted</param>
        /// <typeparam name="T">Entity type</typeparam>
        public virtual Expression<Func<T, bool>> CreateByIdExpression<T>(object id)
            where T : class
        {
            EntityMap entityMap = EntityMapper.GetEntityMap(typeof(T));
            var primaryKey = entityMap.PrimaryKeyPropertiesMetadata.FirstOrDefault();

            if (primaryKey == null)
                throw new NotSupportedException("No primary keys defined");

            var entityKey = new EntityKey(primaryKey, id);

            return CreateByIdExpression<T>(new List<EntityKey>() { entityKey });
        }

        /// <summary>
        /// Creates an search expression for the ID
        /// </summary>
        /// <param name="keys">The keys to search ofor</param>
        /// <typeparam name="T">Entity type</typeparam>
        public virtual Expression<Func<T, bool>> CreateByIdExpression<T>(IEnumerable<EntityKey> keys)
            where T : class
        {
            Expression<Func<T, bool>> returnExpr = null;
            ParameterExpression t = Expression.Parameter(typeof(T), "t");

            foreach (var key in keys)
            {
                Expression keyProperty = Expression.Property(t, key.Property.Name);
                Expression comparison = this.MapKeyValueComparison(keyProperty, key);

                if (returnExpr == null)
                {
                    returnExpr = Expression.Lambda<Func<T, bool>>(comparison, t);
                }
                else
                {
                    var binaryExpression = Expression.AndAlso(returnExpr, Expression.Lambda<Func<T, bool>>(comparison, t));
                    returnExpr = Expression.Lambda<Func<T, bool>>(binaryExpression, returnExpr.Parameters);
                }
            }

            return returnExpr;
        }

        /// <summary>
        /// Maps the correct value to the Expression
        /// </summary>
        /// <param name="keyProperty"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual Expression MapKeyValueComparison(Expression keyProperty, EntityKey key)
        {
            return key.Value switch
            {
                int i => Expression.Equal(keyProperty, Expression.Constant(i)),
                Guid g => Expression.Equal(keyProperty, Expression.Constant(g)),
                string s => Expression.Equal(keyProperty, Expression.Constant(s)),
                _ => Expression.Equal(keyProperty, Expression.Constant(key.Value)),
            };
        }
    }

    public enum QueryType
    {
        Select,
        Update,
        Delete
    }
}
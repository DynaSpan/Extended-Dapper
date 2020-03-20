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
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql.Generator
{
    public partial class SqlGenerator : ISqlGenerator
    {
        private readonly DatabaseProvider databaseProvider;
        private readonly ISqlQueryProvider sqlProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseProvider">Which database we connect to; defaults to MSSQL</param>
        public SqlGenerator(DatabaseProvider databaseProvider = DatabaseProvider.MSSQL)
        {
            this.databaseProvider = databaseProvider;
            this.sqlProvider      = SqlQueryProviderHelper.GetProvider(databaseProvider);

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
            var entityMap = EntityMapper.GetEntityMap(typeof(T));

            var keyProperty = entityMap.PrimaryKeyProperties.Where(x => x.GetCustomAttribute<AutoValueAttribute>() != null).FirstOrDefault();

            if (keyProperty == null)
                keyProperty = entityMap.PrimaryKeyProperties.FirstOrDefault();

            // Check if we need to convert the id
            if (keyProperty.PropertyType == typeof(Guid) && id.GetType() == typeof(string))
                id = new Guid(id.ToString());

            ParameterExpression t = Expression.Parameter(typeof(T), "t");
            Expression idProperty = Expression.Property(t, keyProperty.Name);
            Expression comparison = Expression.Equal(idProperty, Expression.Constant(id));
            
            return Expression.Lambda<Func<T, bool>>(comparison, t);
        }
    }

    public enum QueryType
    {
        Select,
        Update,
        Delete
    }
}
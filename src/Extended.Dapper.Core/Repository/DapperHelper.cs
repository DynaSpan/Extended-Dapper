using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Dapper;
using Extended.Dapper.Core.Reflection;

namespace Extended.Dapper.Core.Repository
{
    public class DapperHelper
    {
        public static IEnumerable<T> CreateDapperQuery<T>(IDbConnection connection, string query, params Expression<Func<T, object>>[] includes)
        {
            var includeGenerics = ReflectionHelper.GetTypeListFromIncludes(includes);

            if (includeGenerics.Count == 1)
                return connection.Query<T>(query, includes);
            
            // switch (includeGenerics.Count)
            // {
            //     case 2:
            //         return connection.Query
            // }

            var queryMethod = typeof(SqlMapper).GetMethod("QueryAsync", new [] { typeof(string), typeof(Type[]) });
            var queryRef = queryMethod.MakeGenericMethod(includeGenerics.ToArray());

            return queryRef.Invoke(connection, new object[] { query, includes }) as IEnumerable<T>;
        }
    }
}
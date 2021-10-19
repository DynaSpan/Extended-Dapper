using System;
using System.Collections.Generic;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public static class SqlQueryProviderHelper
    {
        private static Dictionary<DatabaseProvider, ISqlQueryProvider> providerCache;

        public static bool Verbose { get; set; } = false;

        /// <summary>
        /// Returns an instance of the correct ISqlProvider
        /// </summary>
        /// <param name="databaseProvider"></param>
        public static ISqlQueryProvider GetProvider(DatabaseProvider databaseProvider)
        {
            if (providerCache == null)
                providerCache = new Dictionary<DatabaseProvider, ISqlQueryProvider>();
            else if (providerCache.ContainsKey(databaseProvider))
                return providerCache[databaseProvider];

            ISqlQueryProvider queryProvider = databaseProvider switch
            {
                DatabaseProvider.MSSQL => new MsSqlQueryProvider(databaseProvider),
                DatabaseProvider.MySQL => new MySqlQueryProvider(databaseProvider),
                DatabaseProvider.SQLite => new SqliteQueryProvider(databaseProvider),
                _ => throw new NotImplementedException(),
            };

            providerCache.Add(databaseProvider, queryProvider);

            return queryProvider;
        }
    }
}
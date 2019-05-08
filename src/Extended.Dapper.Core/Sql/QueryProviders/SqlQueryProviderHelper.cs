using System;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public static class SqlQueryProviderHelper
    {
        private static SqlQueryProvider sqlQueryProvider;

        /// <summary>
        /// Sets the database provider, so the correct SqlQueryProvider is
        /// returned when GetProvider() is called
        /// </summary>
        /// <param name="databaseProvider"></param>
        public static void SetProvider(DatabaseProvider databaseProvider)
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.MSSQL:
                    sqlQueryProvider = new MsSqlQueryProvider();
                    break;
                case DatabaseProvider.MySQL:
                    sqlQueryProvider = new MySqlQueryProvider();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns an instance of the correct ISqlProvider
        /// </summary>
        /// <returns>Instance of ISqlProvider according to the databaseProvider; 
        /// or null if not implemented</returns>
        public static ISqlQueryProvider GetProvider()
        {
            return sqlQueryProvider;
        }
    }
}
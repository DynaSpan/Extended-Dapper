using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public static class SqlQueryProviderHelper
    {
        /// <summary>
        /// Returns an instance of the correct ISqlProvider
        /// </summary>
        /// <param name="databaseProvider"></param>
        /// <returns>Instance of ISqlProvider according to the databaseProvider; 
        /// or null if not implemented</returns>
        public static ISqlQueryProvider GetProvider(DatabaseProvider databaseProvider)
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.MSSQL:
                    return new MsSqlQueryProvider();
                case DatabaseProvider.MySQL:
                    return new MySqlQueryProvider();
                default:
                    return null;
            }
        }
    }
}
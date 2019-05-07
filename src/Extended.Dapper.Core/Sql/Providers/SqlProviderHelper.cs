using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.Providers
{
    public static class SqlProviderHelper
    {
        /// <summary>
        /// Returns an instance of the correct ISqlProvider
        /// </summary>
        /// <param name="databaseProvider"></param>
        /// <returns></returns>
        public static ISqlProvider GetProvider(DatabaseProvider databaseProvider)
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.MSSQL:
                    return new MsSqlProvider();
                case DatabaseProvider.MySQL:
                    return new MySqlProvider();
                default:
                    return null;
            }
        }
    }
}
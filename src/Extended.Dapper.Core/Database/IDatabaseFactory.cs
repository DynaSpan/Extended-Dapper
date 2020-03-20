using System.Data;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Database 
{
    public interface IDatabaseFactory
    {
        /// <summary>
        /// Creates a new database connection
        /// </summary>
        IDbConnection GetDatabaseConnection();

        /// <summary>
        /// The type of database
        /// </summary>
        DatabaseProvider DatabaseProvider { get; }

        /// <summary>
        /// The correct SQL provider for the connection
        /// </summary>
        ISqlQueryProvider SqlProvider { get; }
    }
}
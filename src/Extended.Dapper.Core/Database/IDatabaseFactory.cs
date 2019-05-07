using System.Data;

namespace Extended.Dapper.Core.Database 
{
    public interface IDatabaseFactory
    {
        /// <summary>
        /// Creates a new database connection
        /// </summary>
        /// <returns></returns>
        IDbConnection GetDatabaseConnection();
    }
}
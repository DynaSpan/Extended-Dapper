using System.Data;

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
    }
}
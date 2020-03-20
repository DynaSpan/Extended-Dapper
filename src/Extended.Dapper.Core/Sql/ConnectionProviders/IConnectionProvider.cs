using System.Data;

namespace Extended.Dapper.Core.Sql.ConnectionProviders
{
    public interface IConnectionProvider
    {
        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        IDbConnection GetConnection();
    }
}
using System;
using System.Data;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Sql.Generator;

namespace Extended.Dapper.Core.Sql.QueryExecuter
{
    public partial class QueryExecuter : IQueryExecuter
    {
        protected IDatabaseFactory DatabaseFactory { get; set; }
        protected ISqlGenerator SqlGenerator { get; set; }

        public QueryExecuter(IDatabaseFactory databaseFactory, ISqlGenerator sqlGenerator)
        {
            this.DatabaseFactory = databaseFactory;
            this.SqlGenerator = sqlGenerator;
        }

        /// <summary>
        /// Opens the provided connection
        /// </summary>
        /// <param name="connection"></param>
        protected virtual void OpenConnection(IDbConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();

                if (connection.State != System.Data.ConnectionState.Open)
                    throw new ApplicationException("Could not connect to the SQL server");
            }
        }
    }
}
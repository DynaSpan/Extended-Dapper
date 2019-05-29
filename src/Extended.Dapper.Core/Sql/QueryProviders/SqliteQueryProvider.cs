using System;
using System.Data;
using System.Data.SQLite;
using System.Text;
using Extended.Dapper.Core.Database;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class SqliteQueryProvider : SqlQueryProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataSource">Location of the .db file</param>
        public SqliteQueryProvider(string dataSource)
        {
            this.ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = dataSource }.ToString();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbSettings">Use the "database" field to locate the .db file</param>
        public SqliteQueryProvider(DatabaseSettings dbSettings)
        {
            this.ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = dbSettings.Database }.ToString();
        }

        /// <summary>
        /// The char used for parameters
        /// </summary>
        public override string ParameterChar { get { return "$"; } }

        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        public override string EscapeTable(string tableName)
        {
            return "\"" + tableName + "\"";
        }

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public override string EscapeColumn(string columnName)
        {
            return "\"" + columnName + "\"";
        }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
        {
            return new SQLiteConnection(this.ConnectionString);
        }

        /// <summary>
        /// - NOT IMPLEMENTED FOR SQLite -
        /// </summary>
        public override string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            throw new NotImplementedException();
        }
    }
}
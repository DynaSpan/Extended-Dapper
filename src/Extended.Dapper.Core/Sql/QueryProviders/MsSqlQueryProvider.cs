using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class MsSqlQueryProvider : SqlQueryProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseSettings"></param>
        public MsSqlQueryProvider(DatabaseSettings databaseSettings) : base(databaseSettings)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public MsSqlQueryProvider(string connectionString) : base(connectionString)
        {

        }

        /// <summary>
        /// The char used for parameters
        /// </summary>
        public override string ParameterChar { get { return "@"; } }

        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        public override string EscapeTable(string tableName)
        {
            return "[" + tableName + "]";
        }

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public override string EscapeColumn(string columnName)
        {
            return "[" + columnName + "]";
        }

        /// <summary>
        /// Returns a new IDbConnection
        /// </summary>
        public override IDbConnection GetConnection()
        {
            return new SqlConnection(this.ConnectionString);
        }

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        public override string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            StringBuilder connStringBuilder = new StringBuilder();

            if (databaseSettings.Port != null)
                connStringBuilder.AppendFormat("Server={0},{1};", databaseSettings.Host, databaseSettings.Port);
            else
                connStringBuilder.AppendFormat("Server={0};", databaseSettings.Host);
            
            if (databaseSettings.Database != null)
                connStringBuilder.AppendFormat("Database={0};", databaseSettings.Database);
                
            connStringBuilder.AppendFormat("User Id={0};", databaseSettings.User);
            connStringBuilder.AppendFormat("Password={0};", databaseSettings.Password);

            if (databaseSettings.TrustedConnection != null && databaseSettings.TrustedConnection == true) // doesnt work without implicit true
                connStringBuilder.Append("Trusted_Connection=true;");

            return connStringBuilder.ToString();
        }

        /// <summary>
        /// Build a select query
        /// </summary>
        /// <param name="selectQuery"></param>
        public override string BuildSelectQuery(SelectSqlQuery selectQuery)
        {
            var query = new StringBuilder();

            var selectFields = string.Join(", ", selectQuery.Select.Select(this.MapAliasColumn));
            
            if (selectQuery.Limit != null)
                query.AppendFormat("SELECT TOP {0} {1} FROM {2}", selectQuery.Limit, selectFields, this.EscapeTable(selectQuery.From));
            else
                query.AppendFormat("SELECT {0} FROM {1}", selectFields, this.EscapeTable(selectQuery.From));

            if (selectQuery.Joins != null && selectQuery.Joins.Count > 0)
                query.Append(" " + string.Join(" ", selectQuery.Joins.Select(j => this.MapJoin(j, EntityMapper.GetEntityMap(j.EntityType)))));

            if (selectQuery.Where != null && !string.IsNullOrEmpty(selectQuery.Where.ToString()))
                query.AppendFormat(" WHERE {0}", selectQuery.Where);

            if (selectQuery.OrderBy != null && selectQuery.OrderBy.Count > 0)
                query.AppendFormat(" ORDER BY {0}", this.MapOrderBy(selectQuery.OrderBy));

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(query.ToString());

            return query.ToString();
        }
    }
}
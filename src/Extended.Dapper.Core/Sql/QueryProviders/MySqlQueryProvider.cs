using System;
using System.Linq;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class MySqlQueryProvider : SqlQueryProvider
    {
        public MySqlQueryProvider(DatabaseProvider dbProvider) : base(dbProvider)
        { }

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
            return "`" + tableName + "`";
        }

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public override string EscapeColumn(string columnName)
        {
            return "`" + columnName + "`";
        }

        /// <summary>
        /// Builds an insert query
        /// </summary>
        /// <param name="insertQuery"></param>
        public override string BuildInsertQuery(InsertSqlQuery insertQuery)
        {
            var insertFields = string.Join(", ", insertQuery.Insert.Select(this.MapInsertAliasColumn));
            var insertParams = string.Join(", ", insertQuery.Insert.Select(i => this.ParameterChar + i.ParameterName));

            var queryBuilder = new StringBuilder();
            queryBuilder.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2})", this.EscapeTable(insertQuery.Table), insertFields, insertParams);

            if (insertQuery.AutoIncrementKey)
                queryBuilder.Append("; SELECT LAST_INSERT_ID();");

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(queryBuilder.ToString());

            return queryBuilder.ToString();
        }
    }
}
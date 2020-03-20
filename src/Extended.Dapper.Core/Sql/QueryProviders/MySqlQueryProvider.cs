using Extended.Dapper.Core.Database;

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
    }
}
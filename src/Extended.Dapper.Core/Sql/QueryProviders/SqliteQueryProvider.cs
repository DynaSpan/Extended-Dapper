using System;
using System.Data;
using System.Linq;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class SqliteQueryProvider : SqlQueryProvider
    {
        public SqliteQueryProvider(DatabaseProvider dbProvider) : base(dbProvider)
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
                queryBuilder.Append("; SELECT last_insert_rowid();");

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(queryBuilder.ToString());

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Maps the update columns for the query
        /// </summary>
        /// <param name="updateQuery"></param>
        public override string MapUpdateColumn(UpdateSqlQuery updateQuery) => string.Join(", ", updateQuery.Updates.Select(x => {
            return string.Format("{0} = {1}{2}",
                this.EscapeColumn(x.Field),
                this.ParameterChar,
                x.ParameterName);
        }));

        /// <summary>
        /// Maps a logical delete
        /// </summary>
        /// <param name="deleteQuery"></param>
        /// <param name="queryBuilder"></param>
        public override void MapLogicalDelete(DeleteSqlQuery deleteQuery, StringBuilder queryBuilder)
        {
            queryBuilder.AppendFormat("UPDATE {0} SET {1} = 1",
                this.EscapeTable(deleteQuery.Table),
                this.EscapeColumn(deleteQuery.LogicalDeleteField));

            if (deleteQuery.UpdatedAtField != null && deleteQuery.UpdatedAtField != string.Empty)
            {
                queryBuilder.AppendFormat(", {0} = {1}p_updatedat",
                    this.EscapeColumn(deleteQuery.UpdatedAtField),
                    this.ParameterChar);

                if (!deleteQuery.Params.ContainsKey("p_updatedat"))
                    deleteQuery.Params.Add(this.ParameterChar + "p_updatedat", DateTime.UtcNow);
            }
        }
    }
}
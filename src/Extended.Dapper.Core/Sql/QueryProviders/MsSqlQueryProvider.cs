using System;
using System.Data;
using System.Linq;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public class MsSqlQueryProvider : SqlQueryProvider
    {
        public MsSqlQueryProvider(DatabaseProvider dbProvider) : base(dbProvider)
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
            return $"[{tableName}]";
        }

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public override string EscapeColumn(string columnName)
        {
            return $"[{columnName}]";
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

            if (selectQuery.Joins?.Count > 0)
                query.Append(' ').AppendJoin(" ", selectQuery.Joins.Select(j => this.MapJoin(j, EntityMapper.GetEntityMap(j.EntityType))));

            if (selectQuery.Where != null && !string.IsNullOrEmpty(selectQuery.Where.ToString()))
                query.AppendFormat(" WHERE {0}", selectQuery.Where);

            if (selectQuery.OrderBy?.Count > 0)
                query.AppendFormat(" ORDER BY {0}", this.MapOrderBy(selectQuery.OrderBy));

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(query.ToString());

            return query.ToString();
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

            if (insertQuery.AutoIncrementKey)
                queryBuilder.Append("DECLARE @IDs TABLE(Id INT);");

            queryBuilder.AppendFormat("INSERT INTO {0} ({1}) ", this.EscapeTable(insertQuery.Table), insertFields);

            if (insertQuery.AutoIncrementKey)
                queryBuilder.AppendFormat(" OUTPUT INSERTED.{0} INTO @IDs(Id) ", this.EscapeColumn(insertQuery.AutoIncrementField.ColumnName));

            queryBuilder.AppendFormat("VALUES ({0});", insertParams);

            if (insertQuery.AutoIncrementKey)
                queryBuilder.Append("SELECT Id FROM @IDs;");

            if (SqlQueryProviderHelper.Verbose)
                Console.WriteLine(queryBuilder.ToString());

            return queryBuilder.ToString();
        }
    }
}
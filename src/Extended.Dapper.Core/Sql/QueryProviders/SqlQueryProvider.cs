using System.Linq;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;

namespace Extended.Dapper.Core.Sql.QueryProviders
{
    public abstract class SqlQueryProvider : ISqlQueryProvider
    {
        /// <summary>
        /// Escapes a table name in the correct format
        /// </summary>
        /// <param name="tableName"></param>
        public abstract string EscapeTable(string tableName);

        /// <summary>
        /// Escapes a column in the correct format
        /// </summary>
        /// <param name="columnName"></param>
        public abstract string EscapeColumn(string columnName);

        /// <summary>
        /// Builds a connection string
        /// </summary>
        /// <param name="databaseSettings"></param>
        public abstract string BuildConnectionString(DatabaseSettings databaseSettings);

        /// <summary>
        /// Generates the SQL select fields for a given entity
        /// </summary>
        /// <param name="entityMap"></param>
        /// <returns>Fields for in a SELECT query</returns>
        public virtual string GenerateSelectFields(EntityMap entityMap)
        {
            // Projection function
            string MapAliasColumn(SqlPropertyMetadata p)
            {
                if (!string.IsNullOrEmpty(p.ColumnAlias))
                    return string.Format("{0}.{1} AS {2}", 
                        this.EscapeTable(entityMap.TableName), 
                        this.EscapeColumn(p.ColumnName), 
                        this.EscapeColumn(p.PropertyName));
                else 
                    return string.Format("{0}.{1}", 
                        this.EscapeTable(entityMap.TableName), 
                        this.EscapeColumn(p.ColumnName));
            }

            return string.Join(", ", entityMap.MappedPropertiesMetadata.Select(MapAliasColumn));
        }
    }
}
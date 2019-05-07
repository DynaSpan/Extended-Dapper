using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Providers;

namespace Extended.Dapper.Core.Helpers
{
    public class SqlGeneratorHelper
    {
        /// <summary>
        /// Generates the SQL select fields for a given entity
        /// </summary>
        /// <param name="entityMap"></param>
        /// <returns></returns>
        public static string GenerateSelectFields(EntityMap entityMap, ISqlProvider sqlProvider)
        {
            // Projection function
            string MapAliasColumn(SqlPropertyMetadata p)
            {
                if (!string.IsNullOrEmpty(p.ColumnAlias))
                    return string.Format("{0}.{1} AS {2}", 
                        sqlProvider.EscapeTable(entityMap.TableName), 
                        sqlProvider.EscapeColumn(p.ColumnName), 
                        sqlProvider.EscapeColumn(p.PropertyName));
                else 
                    return string.Format("{0}.{1}", 
                        sqlProvider.EscapeTable(entityMap.TableName), 
                        sqlProvider.EscapeColumn(p.ColumnName));
            }

            return string.Join(", ", entityMap.MappedPropertiesMetadata.Select(MapAliasColumn));
        }
    }
}
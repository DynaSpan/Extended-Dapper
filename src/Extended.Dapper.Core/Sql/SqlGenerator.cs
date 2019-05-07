using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Extended.Dapper.Attributes.Entities;
using Extended.Dapper.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Sql
{
    public class SqlGenerator : ISqlGenerator
    {
        private readonly DatabaseProvider databaseProvider;
        private readonly ISqlQueryProvider sqlProvider;

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseProvider">Which database we connect to; defaults to MSSQL</param>
        public SqlGenerator(DatabaseProvider databaseProvider = DatabaseProvider.MSSQL)
        {
            this.databaseProvider = databaseProvider;
            this.sqlProvider      = SqlQueryProviderHelper.GetProvider(databaseProvider);

            // Check if it is implemented
            if (this.sqlProvider == null)
                throw new NotImplementedException();
        }

        #endregion

        #region Insert implementation

        public static string Insert<T>(T entity)
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));

            if (entityMap.UpdatedAtProperty != null)
                entityMap.UpdatedAtProperty.SetValue(entity, DateTime.UtcNow);

            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", 
                entityMap.TableName,
                string.Join(", ", entityMap.MappedPropertiesMetadata.Select(p => p.ColumnName),
                string.Join(", ", entityMap.MappedPropertiesMetadata.Select(p => p.ColumnName))));
        }

        #endregion

        #region Select implementation

        public string Select<T>(Expression<Func<T, bool>> predicate)
        {
            var entityMap = EntityMapper.GetEntityMap(typeof(T));
            var sqlBuilder = new StringBuilder();
            var joinBuilder = new StringBuilder();

            sqlBuilder.AppendFormat("SELECT {0}", this.sqlProvider.GenerateSelectFields(entityMap));

            if (entityMap.RelationPropertiesMetadata != null && entityMap.RelationPropertiesMetadata.Count > 0)
            {
                foreach (SqlRelationPropertyMetadata metadata in entityMap.RelationPropertiesMetadata)
                {
                    var relationEntityMap = EntityMapper.GetEntityMap(metadata.PropertyInfo.GetType());

                    sqlBuilder.AppendFormat(", {0}", this.sqlProvider.GenerateSelectFields(relationEntityMap));

                    // Check the type of relation
                    var relationAttr = metadata.PropertyInfo.GetCustomAttribute<RelationAttributeBase>();

                    string joinType = string.Empty;

                    if (relationAttr is ManyToOneAttribute)
                        joinType = "INNER JOIN";
                    else if (relationAttr is OneToManyAttribute)
                        joinType = "LEFT JOIN";

                    joinBuilder.AppendFormat("{0} {1} ON {2}.{3} = {4}.{5}",
                        joinType,
                        this.sqlProvider.EscapeTable(relationAttr.TableName),
                        this.sqlProvider.EscapeTable(entityMap.TableName),
                        this.sqlProvider.EscapeColumn(relationAttr.LocalKey),
                        this.sqlProvider.EscapeTable(relationAttr.TableName),
                        this.sqlProvider.EscapeColumn(relationAttr.ExternalKey));
                }
            }

            sqlBuilder.AppendFormat("FROM {0} ", this.sqlProvider.EscapeTable(entityMap.TableName));
            sqlBuilder.Append(joinBuilder.ToString());

            return sqlBuilder.ToString();
        }

        #endregion
    }

    public enum QueryType
    {
        Select,
        Update,
        Delete
    }
}
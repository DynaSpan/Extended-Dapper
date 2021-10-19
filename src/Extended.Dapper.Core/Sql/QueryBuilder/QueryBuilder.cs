using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Extended.Dapper.Core.Helpers;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Metadata;
using Extended.Dapper.Core.Sql.Query.Models;
using Extended.Dapper.Core.Sql.QueryExecuter;

namespace Extended.Dapper.Core.Sql.QueryBuilders
{
    public class QueryBuilder<T> where T : class
    {
        protected readonly IQueryExecuter queryExecuter;
        public List<Expression<Func<T, object>>> Selects { get; set; }
        public List<Expression<Func<T, bool>>> Wheres { get; set; }
        public List<IIncludedChild> IncludedChildren { get; set; }
        public Dictionary<Expression<Func<T, object>>, OrderBy> OrderBys { get; set; }
        public int? LimitResults { get; set; }

        public QueryBuilder(IQueryExecuter queryExecuter)
        {
            this.queryExecuter = queryExecuter;

            this.Selects            = new List<Expression<Func<T, object>>>();
            this.Wheres             = new List<Expression<Func<T, bool>>>();
            this.IncludedChildren   = new List<IIncludedChild>();
            this.OrderBys           = new Dictionary<Expression<Func<T, object>>, OrderBy>();
        }

        /// <summary>
        /// Selects a property from the database
        /// </summary>
        /// <param name="selectProperty">The property to select</param>
        public QueryBuilder<T> Select(Expression<Func<T, object>> selectProperty)
        {
            this.Selects.Add(selectProperty);

            return this;
        }

        /// <summary>
        /// Selects multiple properties from the database
        /// </summary>
        /// <param name="selectProperties">Properties to select</param>
        public QueryBuilder<T> Select(params Expression<Func<T, object>>[] selectProperties)
        {
            this.Selects.AddRange(selectProperties);

            return this;
        }

        /// <summary>
        /// Adds a where clause to the query
        /// </summary>
        /// <param name="search"></param>
        public QueryBuilder<T> Where(Expression<Func<T, bool>> search)
        {
            this.Wheres.Add(search);

            return this;
        }

        /// <summary>
        /// Includes one child.
        /// CANNOT BE USED WITH LIMIT
        /// </summary>
        /// <param name="children"></param>
        /// <param name="includeProperties">Which properties to include from the child</param>
        public QueryBuilder<T> IncludeChild<TChild>(Expression<Func<T, object>> child, params Expression<Func<TChild, object>>[] includeProperties)
        {
            this.IncludedChildren.Add(new IncludedChild<TChild>() {
                Child = child,
                IncludedProperties = includeProperties
            });

            return this;
        }

        /// <summary>
        /// Maps a field to order the results by
        /// </summary>
        /// <param name="orderProperty"></param>
        /// <param name="orderBy"></param>
        public QueryBuilder<T> OrderBy(Expression<Func<T, object>> orderProperty, OrderBy orderBy = QueryBuilders.OrderBy.Asc)
        {
            this.OrderBys.Add(orderProperty, orderBy);

            return this;
        }

        /// <summary>
        /// Limits the returned results.
        /// CANNOT BE USED WITH INCLUDECHILDREN
        /// </summary>
        /// <param name="limit"></param>
        public QueryBuilder<T> Limit(int limit)
        {
            this.LimitResults = limit;

            return this;
        }

        /// <summary>
        /// Executes the query and returns the result
        /// </summary>
        public Task<IEnumerable<T>> GetResults()
            => this.queryExecuter.ExecuteQueryBuilder<T>(this);

        public class IncludedChild<TChild> : IIncludedChild
        {
            public Type Type { get => typeof(T); }

            public Type ChildType { get => typeof(TChild); }

            public Expression<Func<T, object>> Child { get; set; }

            public Expression<Func<TChild, object>>[] IncludedProperties { get; set; }

            public string GetMemberName()
                => ((MemberExpression)Child.Body).Member.Name;

            public IEnumerable<SelectField> GetSelectFields(EntityMap entityMap, ICollection<SqlRelationPropertyMetadata> metadata, string tableAlias = null)
            {
                var selectList = new List<SelectField>
                {
                    new SelectField()
                    {
                        IsMainKey = true,
                        Table = entityMap.TableName,
                        TableAlias = tableAlias,
                        Field = "Split_" + entityMap.TableName
                    }
                };

                List<SqlPropertyMetadata> mappedSelects = metadata.Cast<SqlPropertyMetadata>().ToList();

                if (IncludedProperties?.Count() > 0)
                {
                    // TODO: make sure select with multiple tables works properly (check select)
                    mappedSelects = mappedSelects
                        .Where(p => IncludedProperties
                            .Any(f => ExpressionHelper.GetPropertyName(f) == p.PropertyName)).ToList();

                    mappedSelects.AddRange(entityMap.PrimaryKeyPropertiesMetadata);
                }

                selectList.AddRange(mappedSelects.Select(k =>
                    new SelectField(){
                        IsMainKey = false,
                        Table = entityMap.TableName,
                        TableAlias = tableAlias,
                        Field = k.ColumnName,
                        FieldAlias = k.ColumnAlias
                    }
                ));

                return selectList;
            }
        }

        public interface IIncludedChild
        {
            Type Type { get; }

            Type ChildType { get; }

            Expression<Func<T, object>> Child { get; set; }

            string GetMemberName();

            IEnumerable<SelectField> GetSelectFields(EntityMap entityMap, ICollection<SqlRelationPropertyMetadata> metadata, string tableAlias = null);
        }
    }

    public enum OrderBy
    {
        Asc,
        Desc
    }
}
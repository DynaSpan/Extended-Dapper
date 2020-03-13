using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Extended.Dapper.Sql.QueryExecuter;

namespace Extended.Dapper.Core.Sql.QueryBuilder
{
    public class QueryBuilder<T> where T : class
    {
        protected readonly IQueryExecuter queryExecuter;
        public IList<Expression<Func<T, object>>> Selects { get; set; }
        public IList<Expression<Func<T, bool>>> Wheres { get; set; }
        public IList<Expression<Func<T, object>>> IncludedChildren { get; set; }
        public IDictionary<Expression<Func<T, object>>, OrderBy> OrderBys { get; set; }
        public int? LimitResults { get; set; }

        public QueryBuilder(IQueryExecuter queryExecuter) 
        {
            this.queryExecuter = queryExecuter;

            this.Selects            = new List<Expression<Func<T, object>>>();
            this.Wheres             = new List<Expression<Func<T, bool>>>();
            this.IncludedChildren   = new List<Expression<Func<T, object>>>();
            this.OrderBys           = new Dictionary<Expression<Func<T, object>>, OrderBy>();
        }

        // public QueryBuilder<T> Select(Expression<Func<T, object>> selectProperty)
        // {
        //     this.Selects.Add(selectProperty);

        //     return this;
        // }

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
        /// Includes one or more children.
        /// CANNOT BE USED WITH LIMIT
        /// </summary>
        /// <param name="children"></param>
        /// <returns></returns>
        public QueryBuilder<T> IncludeChildren(Expression<Func<T, object>> children)
        {
            this.IncludedChildren.Add(children);

            return this;
        }

        /// <summary>
        /// Maps a field to order the results by
        /// </summary>
        /// <param name="orderProperty"></param>
        /// <param name="orderBy"></param>
        public QueryBuilder<T> OrderBy(Expression<Func<T, object>> orderProperty, OrderBy orderBy = QueryBuilder.OrderBy.Asc)
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
        {
            return this.queryExecuter.ExecuteQueryBuilder<T>(this);
        }
    }

    public enum OrderBy
    {
        Asc,
        Desc
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Extended.Dapper.Sql.QueryExecuter;

namespace Extended.Dapper.Core.Sql.QueryBuilder
{
    public class QueryBuilder<T>
    {
        protected readonly IQueryExecuter queryExecuter;
        protected IList<Expression<Func<T, object>>> Selects { get; set; }
        protected IList<Expression<Func<T, bool>>> Wheres { get; set; }
        protected IList<Expression<Func<T, object>>> IncludedChildren { get; set; }
        protected IDictionary<Expression<Func<T, object>>, OrderBy> OrderBys { get; set; }
        protected int LimitResults { get; set; }

        public QueryBuilder(IQueryExecuter queryExecuter) 
        {
            this.queryExecuter = queryExecuter;

            this.Selects            = new List<Expression<Func<T, object>>>();
            this.Wheres             = new List<Expression<Func<T, bool>>>();
            this.IncludedChildren   = new List<Expression<Func<T, object>>>();
            this.OrderBys           = new Dictionary<Expression<Func<T, object>>, OrderBy>();
        }

        public QueryBuilder<T> Select(Expression<Func<T, object>> selectProperty)
        {
            this.Selects.Add(selectProperty);

            return this;
        }

        public QueryBuilder<T> Where(Expression<Func<T, bool>> search)
        {
            this.Wheres.Add(search);

            return this;
        }

        public QueryBuilder<T> IncludeChildren(Expression<Func<T, object>> children)
        {
            this.IncludedChildren.Add(children);

            return this;
        }

        public QueryBuilder<T> OrderBy(Expression<Func<T, object>> orderProperty, OrderBy orderBy)
        {
            this.OrderBys.Add(orderProperty, orderBy);

            return this;
        }

        public QueryBuilder<T> Limit(int limit)
        {
            this.LimitResults = limit;

            return this;
        }

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
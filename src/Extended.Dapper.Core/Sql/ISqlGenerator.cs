using System;
using System.Linq.Expressions;
using Extended.Dapper.Repositories.Entities;

namespace Extended.Dapper.Core.Sql
{
    public interface ISqlGenerator
    {
        //string Select(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    }
}
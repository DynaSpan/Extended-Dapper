using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;

namespace Extended.Dapper.Core.Reflection
{
    public class ReflectionHelper
    {
        // public static List<Type> GetTypeListFromIncludes<T>(params Expression<Func<T, object>>[] includes)
        // {
        //     var typeList = new List<Type>();
        //     typeList.Add(typeof(T));

        //     Array.ForEach(includes, incl => typeList.Add(incl.Body.Type.GetTypeInfo()));

        //     return typeList;
        // }

        public static List<Type> GetTypeListFromIncludes<T>(Func<object[], T> includes)
        {
            var typeList = new List<Type>();
            typeList.Add(typeof(T));

            //Array.ForEach(includes.Method, incl => typeList.Add(incl.Body.Type.GetTypeInfo()));

            return typeList;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;

namespace Extended.Dapper.Core.Reflection
{
    public class ReflectionHelper
    {
        public static List<Type> GetTypeListFromIncludes<T>(params Expression<Func<T, object>>[] includes)
        {
            var typeList = new List<Type>();
            typeList.Add(typeof(T));

            Array.ForEach(includes, incl => {
                var type = incl.Body.Type.GetTypeInfo();

                if (type.IsGenericType)
                    type = type.GetGenericArguments()[0].GetTypeInfo();

                typeList.Add(type);
            });

            return typeList;
        }

        
        public static List<TList> CloneListAs<TList>(IList<object> source)
        {
            // Here we can do anything we want with T
            // T == source[0].GetType()
            return source.Cast<TList>().ToList();
        }

        // public static List<Type> GetTypeListFromIncludes<T>(Func<object[], T> includes)
        // {
        //     var typeList = new List<Type>();
        //     typeList.Add(typeof(T));

        //     Array.ForEach(includes.Method, incl => typeList.Add(incl.Body.Type.GetTypeInfo()));

        //     return typeList;
        // }
    }
}
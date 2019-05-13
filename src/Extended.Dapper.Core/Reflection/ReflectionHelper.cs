using System;
using System.Collections;
using System.Collections.Concurrent;
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
        /// <summary>
        /// Caches the reference to the CastListAsMethod
        /// </summary>
        private static MethodInfo CastListAsMethod { get; set; }

        /// <summary>
        /// Caches references to generic methods
        /// </summary>
        private static ConcurrentDictionary<string, MethodInfo> GenericMethods { get; set; }

        /// <summary>
        /// Gets all types from the includes expression
        /// </summary>
        /// <param name="includes"></param>
        /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// Calls a generic method with the correct generic
        /// </summary>
        /// <param name="methodClassType">The type of the class that contains the method</param>
        /// <param name="methodName">The name of the method</param>
        /// <param name="genericType">The generic type this method should be called with</param>
        /// <param name="parameters">Any parameters</param>
        /// <returns>Result of method invocation</returns>
        public static object CallGenericMethod(Type methodClassType, string methodName, Type genericType, object[] parameters = null)
        {
            var idStr = methodClassType.ToString() + methodName;
            MethodInfo method;

            if (!GenericMethods.TryGetValue(idStr, out method))
            {
                GenericMethods.TryAdd(idStr, method = methodClassType.GetMethod(methodName));
            }

            return method.MakeGenericMethod(genericType).Invoke(null, parameters);
        }

        /// <summary>
        /// Casts a list from the current type to the
        /// new listType
        /// </summary>
        /// <param name="listType">The new type of the list</param>
        /// <param name="list">The list itself</param>
        public static IList CastListTo(Type listType, IList list)
        {
            if (CastListAsMethod == null)
            {
                // cache
                CastListAsMethod = typeof(ReflectionHelper).GetMethod("CastListAs");
            }

            MethodInfo genericMethod = CastListAsMethod.MakeGenericMethod(listType);

            return genericMethod.Invoke(null, new[] { list }) as IList;
        }
        
        /// <summary>
        /// Casts a list to a new type
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TList"></typeparam>
        public static List<TList> CastListAs<TList>(IList<object> source)
        {
            // Here we can do anything we want with T
            // T == source[0].GetType()
            return source.Cast<TList>().ToList();
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Extended.Dapper.Core.Extensions
{
    /// <summary>
    /// Extensions for all types
    /// </summary>
    /// <author>https://github.com/phnx47/MicroOrm.Dapper.Repositories</author>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Cache types and their info
        /// </summary>
        /// <typeparam name="Type"></typeparam>
        /// <typeparam name="PropertyInfo[]"></typeparam>
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> reflectionPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Retrieves all properties of an entity
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static PropertyInfo[] FindClassProperties(this Type entityType)
        {
            if (reflectionPropertyCache.ContainsKey(entityType))
                return reflectionPropertyCache[entityType];

            var result = entityType.GetProperties().ToArray();

            reflectionPropertyCache.TryAdd(entityType, result);

            return result;
        }

        /// <summary>
        /// Checks if the type is generic
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsGenericType(this Type type)
        {
#if NESTANDARD13
        
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        /// <summary>
        /// Checks if the type is an enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsEnum(this Type type)
        {
#if NESTANDARD13
            return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

        /// <summary>
        /// Checks if the type is a value type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsValueType(this Type type)
        {
#if NESTANDARD13
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        /// <summary>
        /// Checks if the type is a boolean
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsBool(this Type type)
        {
            return type == typeof(bool);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Reflection;

namespace Extended.Dapper.Core.Mappers
{
    public static class DapperMapper
    {
        /// <summary>
        /// Maps a query result set to an entity
        /// </summary>
        /// <param name="typeArr">Array with all sub-types</param>
        /// <param name="lookup">Dictionary to lookup entities</param>
        /// <param name="includes">Which children should be included</param>
        public static Func<object[], T> MapDapperEntity<T>(Dictionary<string, T> lookup, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            return (objectArr) => {
                var entityCompositeKey = EntityMapper.GetEntityKeys<T>((T)objectArr[0]);
                var entityMap          = EntityMapper.GetEntityMap(typeof(T));

                string compositeKey = "";

                foreach (var key in entityCompositeKey)
                    compositeKey = string.Format("{0}{1}={2};", compositeKey, key.Name, key.Value.ToString());

                if (!lookup.TryGetValue(compositeKey, out T entity))
                    lookup.Add(compositeKey, entity = (T)objectArr[0]);

                var singleObjectCacher = new Dictionary<Type, int>();

                foreach (var incl in includes)
                {
                    var exp = (MemberExpression)incl.Body;
                    var type = exp.Type.GetTypeInfo();

                    var property = entityMap.RelationProperties.SingleOrDefault(x => x.Key.Name == exp.Member.Name);

                    if (type.IsGenericType)
                    {
                        // Handle as list, get all entities with this type
                        var listType     = exp.Type.GetGenericType().GetTypeInfo();
                        var listProperty = property.Key.GetValue(entity) as IList;

                        var objList = objectArr.Where(x => x != null && x.GetType() == listType && !EntityMapper.IsAutovalueKeysEmpty(x, x.GetType()));
                        IList value = ReflectionHelper.CastListTo(listType, objList);

                        if (value != null)
                        {
                            if (listProperty == null)
                            {
                                property.Key.SetValue(entity, value);
                            }
                            else
                            {
                                foreach (var val in value)
                                {
                                    if (val != null && !listProperty.Contains(val))
                                    {
                                        listProperty.Add(val);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        object value = null;

                        // Handle as single object
                        if (singleObjectCacher.ContainsKey(type))
                        {
                            var index = ++singleObjectCacher[type];
                            var objArr = objectArr.Where(x => x.GetType() == type);

                            if (objArr.Count() > index)
                                value = objArr.ElementAt(index);
                            else
                                value = objArr.LastOrDefault();
                        }
                        else
                        {
                            value = Array.Find(objectArr, x => x.GetType() == type);
                            singleObjectCacher.Add(type, 0);
                        }

                        if (value != null && !EntityMapper.IsAutovalueKeysEmpty(value, value.GetType()))
                        {
                            property.Key.SetValue(entity, value);
                        }
                    }
                }

                return entity;
            };
        }
    }
}
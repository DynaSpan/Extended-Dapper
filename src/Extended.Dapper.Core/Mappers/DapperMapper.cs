using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Extended.Dapper.Core.Reflection;

namespace Extended.Dapper.Core.Mappers
{
    public class DapperMapper
    {
        /// <summary>
        /// Maps a query result set to an entity
        /// </summary>
        /// <param name="typeArr">Array with all sub-types</param>
        /// <param name="lookup">Dictionary to lookup entities</param>
        /// <param name="includes">Which children should be included</param>
        public static Func<object[], T> MapDapperEntity<T>(Type[] typeArr, Dictionary<string, T> lookup, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            return (objectArr) => {
                T entity;
                var entityCompositeKey  = EntityMapper.GetCompositeUniqueKey((T)objectArr[0]);
                var entityMap           = EntityMapper.GetEntityMap(typeof(T));

                if (!lookup.TryGetValue(entityCompositeKey.ToString(), out entity))
                {
                    lookup.Add(entityCompositeKey.ToString(), entity = (T)objectArr[0]);
                }

                var singleObjectCacher = new Dictionary<Type, int>();

                foreach (var incl in includes)
                {
                    var exp = (MemberExpression)incl.Body;
                    var type = exp.Type.GetTypeInfo();

                    var property = entityMap.RelationProperties.Where(x => x.Key.Name == exp.Member.Name).SingleOrDefault();

                    if (type.IsGenericType)
                    {
                        // Handle as list, get all entities with this type
                        var listType     = type.GetGenericArguments()[0].GetTypeInfo();
                        var listProperty = property.Key.GetValue(entity) as IList;

                        var objList = objectArr.Where(x => x != null && x.GetType() == listType && !EntityMapper.IsKeyEmpty(EntityMapper.GetCompositeUniqueKey(x)));
                        IList value = ReflectionHelper.CastListTo(listType, objList);

                        if (value != null)
                        {
                            if (listProperty == null)
                                property.Key.SetValue(entity, value);
                            else 
                                foreach (var val in value)
                                    if (val != null && !listProperty.Contains(val))
                                        listProperty.Add(val);
                        }
                    }
                    else
                    {
                        object value;

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
                            value = objectArr.Where(x => x.GetType() == type).FirstOrDefault();
                            singleObjectCacher.Add(type, 0);
                        }

                        if (value != null)
                        {
                            var valueId = EntityMapper.GetCompositeUniqueKey(value);

                            if (!EntityMapper.IsKeyEmpty(valueId))
                                property.Key.SetValue(entity, value);
                        }
                    }
                }

                return entity;
            };
        }
    }
}
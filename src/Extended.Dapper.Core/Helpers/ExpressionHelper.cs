using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Extensions;
using Extended.Dapper.Core.Mappers;
using Extended.Dapper.Core.Sql.Query;
using Extended.Dapper.Core.Sql.Query.Expression;
using Extended.Dapper.Core.Sql.QueryProviders;

namespace Extended.Dapper.Core.Helpers
{
    /// <summary>
    /// Helps dealing with Expressions
    /// </summary>
    /// <author>https://github.com/phnx47/MicroOrm.Dapper.Repositories</author>
    internal static class ExpressionHelper
    {
        /// <summary>
        /// Combines multiple expressions into a single Expression by AndAlso
        /// </summary>
        /// <param name="expressions"></param>
        public static Expression<Func<T, bool>> CombineExpressions<T>(IEnumerable<Expression<Func<T, bool>>> expressions)
        {
            Expression<Func<T, bool>> expr1;
            Expression<Func<T, bool>> expr2;

            if (expressions.Count() == 0)
                return null;
            else if (expressions.Count() == 1)
                return expressions.First();
            else if (expressions.Count() == 2)
            {
                expr1 = expressions.ElementAt(0);
                expr2 = expressions.ElementAt(1);
            } else {
                expr1 = expressions.ElementAt(0);
                expr2 = CombineExpressions<T>(expressions.TakeLast(expressions.Count() - 1));
            }

            var body = Expression.AndAlso(expr1.Body, expr2.Body);
            return Expression.Lambda<Func<T,bool>>(body, expr1.Parameters[0]);
        }

        /// <summary>
        /// Gets the name of a property
        /// </summary>
        /// <param name="field"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TField"></typeparam>
        /// <returns>The property name</returns>
        public static string GetPropertyName<TSource, TField>(Expression<Func<TSource, TField>> field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field), "Field can't be null");

            MemberExpression expr;

            switch (field.Body)
            {
                case MemberExpression body:
                    expr = body;
                    break;
                case UnaryExpression expression:
                    expr = (MemberExpression)expression.Operand;
                    break;
                default:
                    throw new ArgumentException("Expression field isn't supported", nameof(field));
            }

            return expr.Member.Name;
        }

        /// <summary>
        /// Gets the value of an expression
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static object GetValue(Expression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        /// <summary>
        /// Converts an Expression to a BinaryExpression
        /// </summary>
        /// <param name="expression">Expression to be converted</param>
        /// <returns></returns>
        public static BinaryExpression GetBinaryExpression(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;

            var body = binaryExpression ?? Expression.MakeBinary(ExpressionType.Equal, expression,
                expression.NodeType == ExpressionType.Not ? Expression.Constant(false) : Expression.Constant(true));

            return body;
        }

        public static Func<PropertyInfo, bool> GetPrimitivePropertiesPredicate()
        {
            return p => p.CanWrite && (p.PropertyType.IsValueType() || p.PropertyType == typeof(string) || p.PropertyType == typeof(byte[]));
        }

        public static object GetValuesFromStringMethod(MethodCallExpression callExpr)
        {
            var expr = callExpr.Method.IsStatic ? callExpr.Arguments[1] : callExpr.Arguments[0];

            return GetValue(expr);
        }

        public static object GetValuesFromCollection(MethodCallExpression callExpr)
        {
            var expr = (callExpr.Method.IsStatic ? callExpr.Arguments.First() : callExpr.Object);

            if (expr.NodeType is ExpressionType.MemberAccess)
            {
                MemberExpression memberExpr = expr as MemberExpression;

                if (!(memberExpr?.Expression is ConstantExpression))
                    throw new NotSupportedException(callExpr.Method.Name + " isn't supported");

                var constExpr = (ConstantExpression)memberExpr.Expression;

                var constExprType = constExpr.Value.GetType();
                return constExprType.GetField(memberExpr.Member.Name).GetValue(constExpr.Value);
            }
            else if (expr.NodeType == ExpressionType.Constant)
            {
                ConstantExpression constantExpr = expr as ConstantExpression;

                return constantExpr.Value;
            }

            return null;
        }

        public static MemberExpression GetMemberExpression(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression expr:
                    if (expr.Method.IsStatic)
                    {
                        Expression memExpr = expr.Arguments.LastOrDefault(x => x.NodeType == ExpressionType.MemberAccess);

                        if (memExpr == null)
                        {
                            UnaryExpression arg = expr.Arguments[1] as UnaryExpression;
                            memExpr = arg.Operand;
                        }

                        return (MemberExpression)memExpr;
                    }
                    else
                        return (MemberExpression)expr.Arguments[0];

                case MemberExpression memberExpression:
                    return memberExpression;

                case UnaryExpression unaryExpression:
                    return (MemberExpression)unaryExpression.Operand;

                case BinaryExpression binaryExpression:
                    var binaryExpr = binaryExpression;

                    if (binaryExpr.Left is UnaryExpression left)
                        return (MemberExpression)left.Operand;

                    //should we take care if right operation is memberaccess, not left?
                    return (MemberExpression)binaryExpr.Left;

                case LambdaExpression lambdaExpression:

                    switch (lambdaExpression.Body)
                    {
                        case MemberExpression body:
                            return body;

                        case UnaryExpression expressionBody:
                            return (MemberExpression)expressionBody.Operand;
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <param name="expr">The Expression.</param>
        /// <param name="nested">Nested property.</param>
        /// <returns>The property name for the property expression.</returns>
        public static string GetPropertyNamePath(Expression expr, out bool nested)
        {
            var path = new StringBuilder();
            var memberExpression = GetMemberExpression(expr);
            var count = 0;
            do
            {
                count++;
                if (path.Length > 0)
                    path.Insert(0, ".");
                path.Insert(0, memberExpression.Member.Name);
                memberExpression = GetMemberExpression(memberExpression.Expression);
            } while (memberExpression != null);

            if (count > 2)
                throw new ArgumentException("Only one degree of nesting is supported");

            nested = count == 2;

            return path.ToString();
        }

        /// <summary>
        /// Get query properties
        /// </summary>
        /// <param name="expr">The expression.</param>
        /// <param name="entityMap"></param>
        public static IList<QueryExpression> GetQueryProperties(Expression expr, EntityMap entityMap, DatabaseProvider dbProvider)
        {
            var queryNode = GetQueryProperties(expr, ExpressionType.Default, entityMap, dbProvider);

            switch (queryNode)
            {
                case QueryParameterExpression qpExpr:
                    return new List<QueryExpression> { queryNode };

                case QueryBinaryExpression qbExpr:
                    return qbExpr.Nodes;

                default:
                    throw new NotSupportedException(queryNode.ToString());
            }
        }

        /// <summary>
        /// Get query properties
        /// </summary>
        /// <param name="expr">The expression.</param>
        /// <param name="linkingType">Type of the linking.</param>
        /// <param name="entityMap"></param>
        private static QueryExpression GetQueryProperties(Expression expr, ExpressionType linkingType, EntityMap entityMap, DatabaseProvider dbProvider)
        {
            var isNotUnary = false;

            if (expr is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Operand is MethodCallExpression)
                {
                    expr = unaryExpression.Operand;
                    isNotUnary = true;
                }
            }

            if (expr is MethodCallExpression methodCallExpression)
            {
                return GetMethodCallExpressionProperties(methodCallExpression, linkingType, entityMap, dbProvider, isNotUnary);
            }

            if (expr is BinaryExpression binaryExpression)
            {
                return GetBinaryExpressionProperties(binaryExpression, linkingType, entityMap, dbProvider);
            }

            return GetQueryProperties(ExpressionHelper.GetBinaryExpression(expr), linkingType, entityMap, dbProvider);
        }

        /// <summary>
        /// Generates the expression for a given MethodCallExpression
        /// </summary>
        /// <param name="methodCallExpression"></param>
        /// <param name="linkingType"></param>
        /// <param name="entityMap"></param>
        /// <param name="isNotUnary"></param>
        /// <param name="methodName"></param>
        internal static QueryParameterExpression GetMethodCallExpressionProperties(
            MethodCallExpression methodCallExpression, 
            ExpressionType linkingType,
            EntityMap entityMap, 
            DatabaseProvider dbProvider,
            bool isNotUnary = false, 
            string methodName = null)
        {
            if (methodName == null)
                methodName = methodCallExpression.Method.Name;
                
            var exprObj = methodCallExpression.Object;
            var sqlQueryProvider = SqlQueryProviderHelper.GetProvider(dbProvider);

            switch (methodName)
            {
                case "Contains":
                {
                    if (exprObj != null
                        && exprObj.NodeType == ExpressionType.MemberAccess
                        && exprObj.Type == typeof(string))
                    {
                        return GetMethodCallExpressionProperties(methodCallExpression, linkingType, entityMap, dbProvider, isNotUnary, "StringContains");
                    }

                    var propertyName = ExpressionHelper.GetPropertyNamePath(methodCallExpression, out var isNested);

                    if (!entityMap.MappedPropertiesMetadata.Any(x => x.PropertyName == propertyName) 
                        && !entityMap.RelationProperties.Any(x => x.Key.Name == propertyName))
                        throw new NotSupportedException("Can't parse the predicate");

                    var propertyValue   = ExpressionHelper.GetValuesFromCollection(methodCallExpression);
                    var opr             = sqlQueryProvider.GetMethodCallSqlOperator(methodName, isNotUnary);
                    var link            = sqlQueryProvider.GetSqlOperator(linkingType);

                    return new QueryParameterExpression(link, propertyName, propertyValue, opr, isNested);
                }
                case "StringContains":
                case "StartsWith":
                case "EndsWith":
                {
                    if (exprObj == null
                        || exprObj.NodeType != ExpressionType.MemberAccess
                        || exprObj.Type != typeof(string))
                    {
                        throw new NotSupportedException($"'{methodName}' method is not supported");
                    }

                    var propertyName = ExpressionHelper.GetPropertyNamePath(exprObj, out bool isNested);

                    if (!entityMap.MappedPropertiesMetadata.Any(x => x.PropertyName == propertyName) 
                        && !entityMap.RelationProperties.Any(x => x.Key.Name == propertyName))
                        throw new NotSupportedException("Can't parse the predicate");

                    var propertyValue   = ExpressionHelper.GetValuesFromStringMethod(methodCallExpression);
                    var likeValue       = sqlQueryProvider.GetSqlLikeValue(methodName, propertyValue);
                    var opr             = sqlQueryProvider.GetMethodCallSqlOperator(methodName, isNotUnary);
                    var link            = sqlQueryProvider.GetSqlOperator(linkingType);

                    return new QueryParameterExpression(link, propertyName, likeValue, opr, isNested);
                }
                default:
                    throw new NotSupportedException($"'{methodName}' method is not supported");
            }
        }

        /// <summary>
        /// Generates the expression for a given BinaryExpression
        /// </summary>
        /// <param name="binaryExpression"></param>
        /// <param name="linkingType"></param>
        /// <param name="entityMap"></param>
        internal static QueryExpression GetBinaryExpressionProperties(
            BinaryExpression binaryExpression,
            ExpressionType linkingType,
            EntityMap entityMap,
            DatabaseProvider dbProvider)
        {
            var sqlQueryProvider = SqlQueryProviderHelper.GetProvider(dbProvider);

            if (binaryExpression.NodeType != ExpressionType.AndAlso && binaryExpression.NodeType != ExpressionType.OrElse)
            {
                var propertyName = ExpressionHelper.GetPropertyNamePath(binaryExpression, out var isNested);

                if (!entityMap.MappedPropertiesMetadata.Any(x => x.PropertyName == propertyName) 
                    && !entityMap.RelationProperties.Any(x => x.Key.Name == propertyName))
                {
                    // Check if the predicate contains a foreign key
                    if (!entityMap.RelationProperties.Any(r => r.Value.Any(p => p.ExternalKey == propertyName))
                        && entityMap.RelationProperties.Any(r => r.Value.Any(p => p.PropertyName == propertyName)))
                        throw new NotSupportedException("Can't parse the predicate");
                }

                var propertyValue   = ExpressionHelper.GetValue(binaryExpression.Right);
                var opr             = sqlQueryProvider.GetSqlOperator(binaryExpression.NodeType);
                var link            = sqlQueryProvider.GetSqlOperator(linkingType);

                return new QueryParameterExpression(link, propertyName, propertyValue, opr, isNested);
            }

            var leftExpr = GetQueryProperties(binaryExpression.Left, ExpressionType.Default, entityMap, dbProvider);
            var rightExpr = GetQueryProperties(binaryExpression.Right, binaryExpression.NodeType, entityMap, dbProvider);

            switch (leftExpr)
            {
                case QueryParameterExpression lQPExpr:
                    if (!string.IsNullOrEmpty(lQPExpr.LinkingOperator) && !string.IsNullOrEmpty(rightExpr.LinkingOperator)) // AND a AND B
                    {
                        switch (rightExpr)
                        {
                            case QueryBinaryExpression rQBExpr:
                                if (lQPExpr.LinkingOperator == rQBExpr.Nodes.Last().LinkingOperator) // AND a AND (c AND d)
                                {
                                    var nodes = new QueryBinaryExpression
                                    {
                                        LinkingOperator = leftExpr.LinkingOperator,
                                        Nodes = new List<QueryExpression> { leftExpr }
                                    };

                                    rQBExpr.Nodes[0].LinkingOperator = rQBExpr.LinkingOperator;
                                    nodes.Nodes.AddRange(rQBExpr.Nodes);

                                    leftExpr = nodes;
                                    rightExpr = null;
                                    // AND a AND (c AND d) => (AND a AND c AND d)
                                }
                                break;
                        }
                    }
                    break;

                case QueryBinaryExpression lQBExpr:
                    switch (rightExpr)
                    {
                        case QueryParameterExpression rQPExpr:
                            if (rQPExpr.LinkingOperator == lQBExpr.Nodes.Last().LinkingOperator)    //(a AND b) AND c
                            {
                                lQBExpr.Nodes.Add(rQPExpr);
                                rightExpr = null;
                                //(a AND b) AND c => (a AND b AND c)
                            }
                            break;

                        case QueryBinaryExpression rQBExpr:
                            if (lQBExpr.Nodes.Last().LinkingOperator == rQBExpr.LinkingOperator) // (a AND b) AND (c AND d)
                            {
                                if (rQBExpr.LinkingOperator == rQBExpr.Nodes.Last().LinkingOperator)   // AND (c AND d)
                                {
                                    rQBExpr.Nodes[0].LinkingOperator = rQBExpr.LinkingOperator;
                                    lQBExpr.Nodes.AddRange(rQBExpr.Nodes);
                                    // (a AND b) AND (c AND d) =>  (a AND b AND c AND d)
                                }
                                else
                                {
                                    lQBExpr.Nodes.Add(rQBExpr);
                                    // (a AND b) AND (c OR d) =>  (a AND b AND (c OR d))
                                }
                                rightExpr = null;
                            }
                            break;
                    }
                    break;
            }

            var nLinkingOperator = sqlQueryProvider.GetSqlOperator(linkingType);
            if (rightExpr == null)
            {
                leftExpr.LinkingOperator = nLinkingOperator;
                return leftExpr;
            }

            return new QueryBinaryExpression
            {
                NodeType = QueryExpressionType.Binary,
                LinkingOperator = nLinkingOperator,
                Nodes = new List<QueryExpression> { leftExpr, rightExpr },
            };
        }
    }
}
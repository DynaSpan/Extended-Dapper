using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Extended.Dapper.Core.Extensions;

namespace Extended.Dapper.Core.Helpers
{
    /// <summary>
    /// Helps dealing with Expressions
    /// </summary>
    /// <author>https://github.com/phnx47/MicroOrm.Dapper.Repositories</author>
    internal static class ExpressionHelper
    {
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
        /// Converts an ExpressionType to a SQL operator
        /// </summary>
        /// <param name="type"></param>
        /// <returns>SQL operator as string</returns>
        public static string GetSqlOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                case ExpressionType.Not:
                case ExpressionType.MemberAccess:
                    return "=";

                case ExpressionType.NotEqual:
                    return "!=";

                case ExpressionType.LessThan:
                    return "<";

                case ExpressionType.LessThanOrEqual:
                    return "<=";

                case ExpressionType.GreaterThan:
                    return ">";

                case ExpressionType.GreaterThanOrEqual:
                    return ">=";

                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    return "AND";

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";

                case ExpressionType.Default:
                    return string.Empty;

                default:
                    throw new NotSupportedException(type + " isn't supported");
            }
        }

        /// <summary>
        /// Gets a value in the correct SQL format
        /// (for LINQ string stuff such as StartsWith, Contains, etc.)
        /// </summary>
        /// <param name="methodName">Name of the LINQ method</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetSqlLikeValue(string methodName, object value)
        {
            if (value == null)
                value = string.Empty;

            switch (methodName)
            {
                case "StartsWith":
                    return string.Format("{0}%", value);

                case "EndsWith":
                    return string.Format("%{0}", value);

                case "StringContains":
                    return string.Format("%{0}%", value);

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the SQL selector for methodName.
        /// </summary>
        /// <param name="methodName">Name of the LINQ method</param>
        /// <param name="isNotUnary">Indicates if the selection should be 
        /// reversed (e.g. IN => NOT IN)</param>
        /// <returns></returns>
        public static string GetMethodCallSqlOperator(string methodName, bool isNotUnary = false)
        {
            switch (methodName)
            {
                case "StartsWith":
                case "EndsWith":
                case "StringContains":
                    return isNotUnary ? "NOT LIKE" : "LIKE";

                case "Contains":
                    return isNotUnary ? "NOT IN" : "IN";

                case "Any":
                case "All":
                    return methodName.ToUpperInvariant();

                default:
                    throw new NotSupportedException(methodName + " isn't supported");
            }
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
            var expr = (callExpr.Method.IsStatic ? callExpr.Arguments.First() : callExpr.Object)
                            as MemberExpression;

            if (!(expr?.Expression is ConstantExpression))
                throw new NotSupportedException(callExpr.Method.Name + " isn't supported");

            var constExpr = (ConstantExpression)expr.Expression;

            var constExprType = constExpr.Value.GetType();
            return constExprType.GetField(expr.Member.Name).GetValue(constExpr.Value);
        }

        public static MemberExpression GetMemberExpression(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression expr:
                    if (expr.Method.IsStatic)
                        return (MemberExpression)expr.Arguments.Last(x => x.NodeType == ExpressionType.MemberAccess);
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
                    path.Insert(0, "");
                path.Insert(0, memberExpression.Member.Name);
                memberExpression = GetMemberExpression(memberExpression.Expression);
            } while (memberExpression != null);

            if (count > 2)
                throw new ArgumentException("Only one degree of nesting is supported");

            nested = count == 2;

            return path.ToString();
        }
    }
}
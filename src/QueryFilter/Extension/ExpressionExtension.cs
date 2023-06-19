using System;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryFilter
{
    public static class ExpressionExtension
    {
        private static readonly MethodInfo trimMethod = typeof(string).GetMethod("Trim", new Type[0]);
        private static readonly MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);

        public static Expression TrimToLower(this MemberExpression member)
        {
            var trimMemberCall = Expression.Call(member, trimMethod);
            return Expression.Call(trimMemberCall, toLowerMethod);
        }

        /// <summary>
        /// Applies the string Trim and ToLower methods to an ExpressionMember.
        /// </summary>
        /// <param name="constant">Constant to which to methods will be applied.</param>
        /// <returns></returns>
        public static Expression TrimToLower(this ConstantExpression constant)
        {
            if (constant.Value == null)
                return constant;

            var trimMemberCall = Expression.Call(constant, trimMethod);
            return Expression.Call(trimMemberCall, toLowerMethod);
        }

        public static Expression TrimToLower(this UnaryExpression constant)
        {
            if (constant == null)
                return constant;

            var trimMemberCall = Expression.Call(constant, trimMethod);
            return Expression.Call(trimMemberCall, toLowerMethod);
        }

        /// <summary>
        /// Adds a "null check" to the expression (before the original one).
        /// </summary>
        /// <param name="expression">Expression to which the null check will be pre-pended.</param>
        /// <param name="member">Member that will be checked.</param>
        /// <returns></returns>
        public static Expression AddNullCheck(this Expression expression, MemberExpression member)
        {
            Expression memberIsNotNull = Expression.NotEqual(member, Expression.Constant(null));
            return Expression.AndAlso(memberIsNotNull, expression);
        }

        public static bool IsGenericList(this object o)
        {
            var oType = o.GetType();
            return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>)));
        }
    }
}
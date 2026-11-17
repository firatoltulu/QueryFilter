namespace QueryFilter
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class ExpressionExtension
    {
        private static readonly MethodInfo TrimMethod = typeof(string).GetMethod("Trim", new Type[0]);
        private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);

        /// <summary>
        /// Applies Trim().ToLower() to a member expression (database column).
        /// ORM providers translate ToLower() to SQL LOWER().
        /// Must use ToLower (not ToLowerInvariant) for ORM SQL translation compatibility.
        /// </summary>
        public static Expression TrimToLower(this MemberExpression member)
        {
            var trimMemberCall = Expression.Call(member, TrimMethod);
            return Expression.Call(trimMemberCall, ToLowerMethod);
        }

        /// <summary>
        /// Applies Trim and ToLowerInvariant to a constant expression value.
        /// Uses InvariantCulture to avoid Turkish-I and similar locale issues
        /// where C# ToLower() produces different results than database Lower().
        /// </summary>
        public static Expression TrimToLower(this ConstantExpression constant)
        {
            if (constant.Value == null)
                return constant;

            var value = constant.Value.ToString().Trim().ToLowerInvariant();
            return Expression.Constant(value);
        }

        /// <summary>
        /// Applies Trim and ToLowerInvariant to a unary (Convert) expression value.
        /// Uses InvariantCulture to avoid Turkish-I and similar locale issues
        /// where C# ToLower() produces different results than database Lower().
        /// </summary>
        public static Expression TrimToLower(this UnaryExpression constant)
        {
            if (constant == null)
                return constant;

            if (constant.Operand is ConstantExpression innerConstant && innerConstant.Value is string strValue)
            {
                return Expression.Constant(strValue.Trim().ToLowerInvariant());
            }

            var trimMemberCall = Expression.Call(constant, TrimMethod);
            return Expression.Call(trimMemberCall, ToLowerMethod);
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
            return oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>));
        }
    }
}

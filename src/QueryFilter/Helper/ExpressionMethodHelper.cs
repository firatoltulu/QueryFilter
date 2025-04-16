namespace QueryFilter
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class ExpressionMethodHelper
    {
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        private static readonly MethodInfo TrimMethod = typeof(string).GetMethod("Trim", new Type[0]);
        private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);
        private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        private static readonly MethodInfo CountMethod = typeof(Enumerable).GetMethods().First(method => method.Name == "Count" && method.GetParameters().Length == 1);
        private static readonly Dictionary<FilterOperator, Func<Expression, Expression, Expression>> Expressions;

        static ExpressionMethodHelper() => Expressions = new Dictionary<FilterOperator, Func<Expression, Expression, Expression>>
            {
                { FilterOperator.IsEqualTo, (member, constant) => Expression.Equal(member, constant) },
                { FilterOperator.IsNotEqualTo, (member, constant) => Expression.NotEqual(member, constant) },
                { FilterOperator.IsGreaterThan, (member, constant) => Expression.GreaterThan(member, constant) },
                { FilterOperator.IsGreaterThanOrEqualTo, (member, constant) => Expression.GreaterThanOrEqual(member, constant) },
                { FilterOperator.IsLessThan, (member, constant) => Expression.LessThan(member, constant) },
                { FilterOperator.IsLessThanOrEqualTo, (member, constant) => Expression.LessThanOrEqual(member, constant) },
                { FilterOperator.StartsWith, (member, constant) => Expression.Call(member, StartsWithMethod, constant) },
                { FilterOperator.NotStartsWith, (member, constant) => Expression.Not(Expression.Call(member, StartsWithMethod, constant)) },
                { FilterOperator.EndsWith, (member, constant) => Expression.Call(member, EndsWithMethod, constant) },
                { FilterOperator.NotEndsWith, (member, constant) => Expression.Not(Expression.Call(member, EndsWithMethod, constant)) },
                { FilterOperator.Contains, (member, constant) => Expression.Call(member, ContainsMethod, constant) },
                { FilterOperator.NotContains, (member, constant) => Expression.Not(Expression.Call(member, ContainsMethod, constant)) },
                { FilterOperator.IsContainedIn, (member, constant) => In(member, constant) },
                { FilterOperator.NotIsContainedIn, (member, constant) => NotIn(member, constant) },
                { FilterOperator.Count, (member, constant) => Expression.Call(member, CountMethod, constant) },
            };

        public static Expression<Func<T, bool>> GetExpression<T>(IList<IFilterDescriptor> filter, FilterCompositionLogicalOperator connector = FilterCompositionLogicalOperator.And) where T : class
        {
            var param = Expression.Parameter(typeof(T), "x");
            Expression expression = Expression.Constant(true);
            foreach (var statement in filter)
            {
                var expr = Visit(param, statement);
                expression = CombineExpressions(expression, expr, connector);
            }

            return Expression.Lambda<Func<T, bool>>(expression, param);
        }

        private static Expression Visit(
            ParameterExpression param,
            IFilterDescriptor ex,
            FilterCompositionLogicalOperator connector = FilterCompositionLogicalOperator.And)
        {
            Expression expression = Expression.Constant(true);
            Expression expr = null;

            if (ex is CompositeFilterDescriptor)
            {
                var compositeFilter = ex as CompositeFilterDescriptor;

                var left = compositeFilter.FilterDescriptors.FirstOrDefault();
                var right = compositeFilter.FilterDescriptors.LastOrDefault();

                var expr1 = Visit(param, left);
                var expr2 = Visit(param, right);

                expr = CombineExpressions(expr1, expr2, compositeFilter.LogicalOperator);
            }
            else if (ex is FilterDescriptor)
            {
                var filter = ex as FilterDescriptor;
                expr = GetExpression(param, filter);
            }

            return CombineExpressions(expression, expr, connector);
        }

        private static Expression CombineExpressions(Expression expr1, Expression expr2, FilterCompositionLogicalOperator connector) => connector == FilterCompositionLogicalOperator.And ? Expression.AndAlso(expr1, expr2) : Expression.OrElse(expr1, expr2);

        private static Expression GetExpression(ParameterExpression param, FilterDescriptor statement, string propertyName = null)
        {
            var member = GetMemberExpression(param, propertyName ?? statement.Member);
            Expression resultExpr = null;

            if (Nullable.GetUnderlyingType(member.Type) != null && statement.Value != null)
            {
                resultExpr = Expression.Property(member, "HasValue");
            }

            var inOperator = new List<FilterOperator>() { FilterOperator.IsContainedIn, FilterOperator.NotIsContainedIn };

            var constant = Expression.Constant(statement.Value);

            var expressionOperator = Expressions[statement.Operator];
            Expression expressionInvoke;

            if (inOperator.IndexOf(statement.Operator) == -1)
            {
                var convertedValue = statement.Value.Convert(member.Type);
                var valueAs = Expression.Convert(Expression.Constant(convertedValue), member.Type);

                if (member.Type == typeof(string))
                {
                    if (constant.Value != null)
                    {
                        expressionInvoke = expressionOperator.Invoke(member.TrimToLower(), valueAs.TrimToLower())
                                                .AddNullCheck(member);
                    }
                    else
                    {
                        expressionInvoke = expressionOperator.Invoke(member, valueAs);
                    }
                }
                else
                {
                    expressionInvoke = Expressions[statement.Operator].Invoke(member, valueAs);
                }
            }
            else if (member.Type != typeof(string))
            {
                expressionInvoke = Expressions[statement.Operator].Invoke(member, constant);
            }
            else
            {
                expressionInvoke = Expressions[statement.Operator].Invoke(member, constant).AddNullCheck(member);
            }

            return resultExpr != null ? Expression.AndAlso(resultExpr, expressionInvoke) : expressionInvoke;
        }

        private static MemberExpression GetMemberExpression(Expression param, string propertyName)
        {
            if (propertyName.Contains("."))
            {
                var index = propertyName.IndexOf(".");
                var subParam = Expression.Property(param, propertyName[..index]);
                return GetMemberExpression(subParam, propertyName[(index + 1)..]);
            }

            return Expression.Property(param, propertyName);
        }

        private static List<ConstantExpression> GetConstants(Type type, object value, bool isCollection)
        {
            if (type == typeof(DateTime))
            {
                DateTime tDate;
                if (isCollection)
                {
                    var vals =
                        value.ToString().Split(new[] { ",", "[", "]", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Select(
                                p =>
                                    DateTime.TryParse(p.Trim(), CultureInfo.InvariantCulture,
                                        DateTimeStyles.AdjustToUniversal, out tDate)
                                        ? (DateTime?)
                                            tDate
                                        : null).Select(p =>
                                            Expression.Constant(p, type));
                    return vals.ToList();
                }

                return new List<ConstantExpression>()
                    {
                        Expression.Constant(DateTime.TryParse(value.ToString().Trim(), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal, out tDate)
                            ? (DateTime?)
                                tDate
                            : null)
                    };
            }
            else if (isCollection)
            {
                var vals = JArray.FromObject(value).Select(p =>
                            Expression.Constant(p.ToObject(type), type));
                return vals.ToList();
            }
            else
            {
                var tc = TypeDescriptor.GetConverter(type);
                return new List<ConstantExpression>()
                {
                    Expression.Constant(tc.ConvertFromString(value.ToString().Trim()))
                };
            }
        }

        private static Expression In(Expression propertyExp, Expression constantExpression)
        {
            var type = propertyExp.Type;
            var value = constantExpression as ConstantExpression;
            var someValues = GetConstants(type, value.Value, true);
            var nullCheck = GetNullCheckExpression(propertyExp);

            Expression exOut;
            if (someValues.Count > 1)
            {
                if (type == typeof(string))
                {
                    exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                    exOut = Expression.Equal(exOut, Expression.Convert(someValues[0], propertyExp.Type));
                    for (var counter = 1; counter < someValues.Count; counter++)
                    {
                        exOut = Expression.Or(exOut,
                            Expression.Equal(
                                Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                                Expression.Convert(someValues[counter], propertyExp.Type)));
                    }
                }
                else
                {
                    exOut = Expression.Equal(propertyExp, Expression.Convert(someValues[0], propertyExp.Type));
                    for (var counter = 1; counter < someValues.Count; counter++)
                    {
                        exOut = Expression.Or(exOut,
                            Expression.Equal(propertyExp, Expression.Convert(someValues[counter], propertyExp.Type)));
                    }
                }
            }
            else if (type == typeof(string))
            {
                exOut = Expression.Call(propertyExp, ToLowerMethod);

                var trimMemberCall = Expression.Call(someValues.First(), TrimMethod);
                var rightEx = Expression.Call(trimMemberCall, ToLowerMethod);

                exOut = Expression.Equal(exOut, rightEx);
            }
            else
            {
                exOut = Expression.Equal(propertyExp, Expression.Convert(someValues[0], propertyExp.Type));
            }
            return Expression.AndAlso(nullCheck, exOut);
        }

        private static Expression GetNullCheckExpression(Expression propertyExp)
        {
            var isNullable = !propertyExp.Type.IsValueType || Nullable.GetUnderlyingType(propertyExp.Type) != null;
            if (isNullable)
            {
                return Expression.NotEqual(propertyExp,
                    Expression.Constant(propertyExp.Type.GetDefaultValue(), propertyExp.Type));
            }
            return Expression.Equal(Expression.Constant(true, typeof(bool)),
                Expression.Constant(true, typeof(bool)));
        }

        private static Expression NotIn(Expression propertyExp, Expression constantExpression) => Expression.Not(In(propertyExp, constantExpression));

        private static object GetDefaultValue(this Type type) => type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;

        public static void CheckPropertyValueMismatch(MemberExpression member, ConstantExpression constant1)
        {
            var memberType = member.Member.MemberType == MemberTypes.Property ? (member.Member as PropertyInfo).PropertyType : (member.Member as FieldInfo).FieldType;

            var constant1Type = GetConstantType(constant1);
            var nullableType = constant1Type != null ? Nullable.GetUnderlyingType(constant1Type) : null;

            var constantValueIsNotNull = constant1.Value != null;
            var memberAndConstantTypeDoNotMatch = nullableType == null && memberType != constant1Type;
            var memberAndNullableUnderlyingTypeDoNotMatch = nullableType != null && memberType != nullableType;

            if (constantValueIsNotNull && (memberAndConstantTypeDoNotMatch || memberAndNullableUnderlyingTypeDoNotMatch))
            {
                throw new ArgumentException($"{member.Member.Name}, {memberType.Name}, {constant1.Type.Name}");
            }
        }

        private static Type GetConstantType(ConstantExpression constant)
        {
            if (constant != null && constant.Value?.IsGenericList() == true)
            {
                return constant.Value.GetType().GenericTypeArguments[0];
            }

            return constant?.Value != null ? constant.Value.GetType() : null;
        }
    }
}

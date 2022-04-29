using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryFilter
{
    public static class ExpressionMethodHelper
    {
        private static MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        private static MethodInfo trimMethod = typeof(string).GetMethod("Trim", new Type[0]);
        private static MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);
        private static MethodInfo startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        private static MethodInfo endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        private static MethodInfo isNullOrEmpty = typeof(string).GetMethod("IsNullOrEmpty", new[] { typeof(string) });
        private static MethodInfo CountMethod = typeof(Enumerable).GetMethods().First(method => method.Name == "Count" && method.GetParameters().Length == 1);

        private static readonly Dictionary<FilterOperator, Func<Expression, Expression, Expression>> Expressions;

        static ExpressionMethodHelper()
        {
            Expressions = new Dictionary<FilterOperator, Func<Expression, Expression, Expression>>();
            Expressions.Add(FilterOperator.IsEqualTo, (member, constant) => Expression.Equal(member, constant));
            Expressions.Add(FilterOperator.IsNotEqualTo, (member, constant) => Expression.NotEqual(member, constant));
            Expressions.Add(FilterOperator.IsGreaterThan, (member, constant) => Expression.GreaterThan(member, constant));
            Expressions.Add(FilterOperator.IsGreaterThanOrEqualTo, (member, constant) => Expression.GreaterThanOrEqual(member, constant));
            Expressions.Add(FilterOperator.IsLessThan, (member, constant) => Expression.LessThan(member, constant));
            Expressions.Add(FilterOperator.IsLessThanOrEqualTo, (member, constant) => Expression.LessThanOrEqual(member, constant));
            Expressions.Add(FilterOperator.StartsWith, (member, constant) => Expression.Call(member, startsWithMethod, constant));
            Expressions.Add(FilterOperator.EndsWith, (member, constant) => Expression.Call(member, endsWithMethod, constant));
            Expressions.Add(FilterOperator.Contains, (member, constant) => Expression.Call(member, containsMethod, constant));
            Expressions.Add(FilterOperator.NotContains, (member, constant) => Expression.Not(Expression.Call(member, containsMethod, constant)));
            Expressions.Add(FilterOperator.IsContainedIn, (member, constant) => In(member, constant));
            Expressions.Add(FilterOperator.Count, (member, constant) => Expression.Call(member, CountMethod, constant));
        }

        public static Expression<Func<T, bool>> GetExpression<T>(IList<IFilterDescriptor> filter, FilterCompositionLogicalOperator connector = FilterCompositionLogicalOperator.And) where T : class
        {
            var param = Expression.Parameter(typeof(T), "x");
            Expression expression = Expression.Constant(true);
            foreach (var statement in filter)
            {
                Expression expr = Visit(param, statement);
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

            expression = CombineExpressions(expression, expr, connector);
            return expression;
        }

        private static Expression CombineExpressions(Expression expr1, Expression expr2, FilterCompositionLogicalOperator connector)
        {
            return connector == FilterCompositionLogicalOperator.And ? Expression.AndAlso(expr1, expr2) : Expression.OrElse(expr1, expr2);
        }

        private static Expression GetExpression(ParameterExpression param, FilterDescriptor statement, string propertyName = null)
        {
            Expression member = GetMemberExpression(param, propertyName ?? statement.Member);
            if (statement.Operator != FilterOperator.IsContainedIn)
            {
                var convertedValue = statement.Value.Convert(member.Type);
                Expression constant = Expression.Convert(Expression.Constant(convertedValue), member.Type);

                if (member.Type == typeof(string))
                {
                    if (convertedValue != null)
                    {
                        var trimMemberCall = Expression.Call(member, trimMethod);
                        member = Expression.Call(trimMemberCall, toLowerMethod);

                        var trimConstantCall = Expression.Call(constant, trimMethod);
                        constant = Expression.Call(trimConstantCall, toLowerMethod);
                    }
                    else
                        constant = Expression.Constant(string.Empty);
                }

                return Expressions[statement.Operator].Invoke(member, constant);
            }
            else
            {
                Expression constant = Expression.Constant(statement.Value);
                return Expressions[statement.Operator].Invoke(member, constant);
            }
        }

        private static Expression Contains(Expression member, Expression expression)
        {
            if (expression is ConstantExpression)
            {
                var constant = (ConstantExpression)expression;
                if (constant.Value is IList && constant.Value.GetType().IsGenericType)
                {
                    var type = constant.Value.GetType();
                    var containsInfo = type.GetMethod("Contains", new[] { type.GetGenericArguments()[0] });
                    var contains = Expression.Call(constant, containsInfo, member);
                    return contains;
                }
            }

            return Expression.Call(member, containsMethod, expression);
        }

        private static MemberExpression GetMemberExpression(Expression param, string propertyName)
        {
            if (propertyName.Contains("."))
            {
                int index = propertyName.IndexOf(".");
                var subParam = Expression.Property(param, propertyName.Substring(0, index));
                return GetMemberExpression(subParam, propertyName.Substring(index + 1));
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
                else
                {
                    return new List<ConstantExpression>()
                    {
                        Expression.Constant(DateTime.TryParse(value.ToString().Trim(), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal, out tDate)
                            ? (DateTime?)
                                tDate
                            : null)
                    };
                }
            }
            else
            {
                if (isCollection)
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
                    var counter = 1;
                    while (counter < someValues.Count)
                    {
                        exOut = Expression.Or(exOut,
                            Expression.Equal(
                                Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                                Expression.Convert(someValues[counter], propertyExp.Type)));
                        counter++;
                    }
                }
                else
                {
                    exOut = Expression.Equal(propertyExp, Expression.Convert(someValues[0], propertyExp.Type));
                    var counter = 1;
                    while (counter < someValues.Count)
                    {
                        exOut = Expression.Or(exOut,
                            Expression.Equal(propertyExp, Expression.Convert(someValues[counter], propertyExp.Type)));
                        counter++;
                    }
                }
            }
            else
            {
                if (type == typeof(string))
                {
                    exOut = Expression.Call(propertyExp, toLowerMethod);

                    var trimMemberCall = Expression.Call(someValues.First(), trimMethod);
                    var rightEx = Expression.Call(trimMemberCall, toLowerMethod);

                    exOut = Expression.Equal(exOut, rightEx);
                }
                else
                {
                    exOut = Expression.Equal(propertyExp, Expression.Convert(someValues.First(), propertyExp.Type));
                }
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

        private static Expression NotIn(Expression propertyExp, Expression constantExpression)
        {
            return Expression.Not(In(propertyExp, constantExpression));
        }

        private static object GetDefaultValue(this Type type)
        {
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
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

        /// <summary>
        /// Gets an expression for filtering based on the provided filter descriptors
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="filter">The list of filter descriptors</param>
        /// <param name="connector">The logical operator to connect the filters</param>
        /// <returns>An expression that can be used for filtering</returns>
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

        /// <summary>
        /// Gets an expression that combines the standard filters with any additional query conditions
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="queryFilterModel">The query filter model containing filters and additional conditions</param>
        /// <param name="connector">The logical operator to connect the filters</param>
        /// <returns>An expression that can be used for filtering</returns>
        public static Expression<Func<T, bool>> GetCombinedExpression<T>(QueryFilterModel queryFilterModel, FilterCompositionLogicalOperator connector = FilterCompositionLogicalOperator.And) where T : class
        {
            // Get the standard filter expression
            var standardExpression = GetExpression<T>(queryFilterModel.FilterDescriptors, connector);
            
            // Get additional query conditions
            var additionalExpressions = queryFilterModel.GetQueryAdditionals<T>()
                .Select(additional => additional.GetExpression())
                .ToList();

            if (additionalExpressions.Count == 0)
            {
                return standardExpression;
            }

            // Combine all expressions
            var combinedExpression = standardExpression;
            foreach (var additionalExpression in additionalExpressions)
            {
                combinedExpression = CombineExpressions(combinedExpression, additionalExpression);
            }

            return combinedExpression;
        }

        /// <summary>
        /// Applies the query filter model to an IQueryable, including any additional query conditions
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="query">The source IQueryable</param>
        /// <param name="queryFilterModel">The query filter model</param>
        /// <returns>The filtered IQueryable</returns>
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, QueryFilterModel queryFilterModel) where T : class
        {
            // Apply standard filters
            if (queryFilterModel.FilterDescriptors.Count > 0)
            {
                var expression = GetExpression<T>(queryFilterModel.FilterDescriptors);
                query = query.Where(expression);
            }

            // Apply additional query conditions
            var additionals = queryFilterModel.GetQueryAdditionals<T>();
            foreach (var additional in additionals)
            {
                query = additional.Apply(query);
            }

            // Apply sorting
            if (queryFilterModel.SortDescriptors.Count > 0)
            {
                query = ApplySorting(query, queryFilterModel.SortDescriptors);
            }

            // Apply paging
            if (queryFilterModel.Skip > 0)
            {
                query = query.Skip(queryFilterModel.Skip);
            }

            if (queryFilterModel.Top > 0)
            {
                query = query.Take(queryFilterModel.Top);
            }

            return query;
        }

        /// <summary>
        /// Applies the query filter model to an IEnumerable, including any additional query conditions
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="source">The source IEnumerable</param>
        /// <param name="queryFilterModel">The query filter model</param>
        /// <returns>The filtered IEnumerable</returns>
        public static IEnumerable<T> ApplyFilter<T>(this IEnumerable<T> source, QueryFilterModel queryFilterModel) where T : class
        {
            // Convert to IQueryable for consistent handling
            var query = source.AsQueryable();
            
            // Apply the filter using the IQueryable extension
            return ApplyFilter(query, queryFilterModel);
        }

        private static Expression CombineExpressions(Expression expr1, Expression expr2, FilterCompositionLogicalOperator connector) => connector == FilterCompositionLogicalOperator.And ? Expression.AndAlso(expr1, expr2) : Expression.OrElse(expr1, expr2);

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

        /// <summary>
        /// Combines two expressions with the specified logical operator
        /// </summary>
        private static Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2) where T : class
        {
            // Create a parameter for the combined expression
            var parameter = Expression.Parameter(typeof(T), "x");
            
            // Replace the parameters in the expressions with the new parameter
            var visitor1 = new ParameterReplacerVisitor(expr1.Parameters[0], parameter);
            var visitor2 = new ParameterReplacerVisitor(expr2.Parameters[0], parameter);
            
            var body1 = visitor1.Visit(expr1.Body);
            var body2 = visitor2.Visit(expr2.Body);
            
            // Combine the bodies with AND
            var combinedBody = Expression.AndAlso(body1, body2);
            
            // Create a new lambda with the combined body
            return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
        }

        /// <summary>
        /// Applies sorting to an IQueryable based on the provided sort descriptors
        /// </summary>
        private static IQueryable<T> ApplySorting<T>(IQueryable<T> query, IList<SortDescriptor> sortDescriptors) where T : class
        {
            if (sortDescriptors.Count == 0)
                return query;

            var firstSort = sortDescriptors[0];
            var param = Expression.Parameter(typeof(T), "x");
            var property = GetMemberExpression(param, firstSort.Member);
            var lambda = Expression.Lambda(property, param);

            // First sort
            var methodName = firstSort.SortDirection == System.ComponentModel.ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            var orderByMethod = typeof(Queryable).GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(typeof(T), property.Type);

            var sortedQuery = (IOrderedQueryable<T>)orderByMethod.Invoke(null, new object[] { query, lambda });

            // Additional sorts
            for (int i = 1; i < sortDescriptors.Count; i++)
            {
                var sort = sortDescriptors[i];
                param = Expression.Parameter(typeof(T), "x");
                property = GetMemberExpression(param, sort.Member);
                lambda = Expression.Lambda(property, param);

                methodName = sort.SortDirection == System.ComponentModel.ListSortDirection.Ascending ? "ThenBy" : "ThenByDescending";
                var thenByMethod = typeof(Queryable).GetMethods()
                    .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .First()
                    .MakeGenericMethod(typeof(T), property.Type);

                sortedQuery = (IOrderedQueryable<T>)thenByMethod.Invoke(null, new object[] { sortedQuery, lambda });
            }

            return sortedQuery;
        }

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

    /// <summary>
    /// Expression visitor that replaces parameters in expressions
    /// </summary>
    internal class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacerVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}

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
    public static class ExpressionSortHelper
    {
        public static Expression<Func<T, object>> GetExpression<T>(string property) where T : class
        {
            var param = Expression.Parameter(typeof(T), "p");
            var parts = property.Split('.');
            Expression parent = param;
            foreach (var part in parts)
            {
                parent = Expression.Property(parent, part);
            }

            if (parent.Type.IsValueType)
            {
                var converted = Expression.Convert(parent, typeof(object));
                return Expression.Lambda<Func<T, object>>(converted, param);
            }
            else
            {
                return Expression.Lambda<Func<T, object>>(parent, param);
            }
        }

        public static IOrderedQueryable<T> ApplySort<T>(this IQueryable<T> source, IList<SortDescriptor> sortDescriptors) where T : class
        {
            var sortedCount = 0;
            IOrderedQueryable<T> result = null;
            if (sortDescriptors.Any())
            {
                foreach (var sortDescriptor in sortDescriptors)
                {
                    Expression<Func<T, object>> sortingExp = ExpressionSortHelper.GetExpression<T>(sortDescriptor.Member);
                    if (sortedCount == 0)
                        if (sortDescriptor.SortDirection == System.ComponentModel.ListSortDirection.Ascending)
                            result = source.OrderBy(sortingExp);
                        else
                            result = source.OrderByDescending(sortingExp);
                    else
                        if (sortDescriptor.SortDirection == System.ComponentModel.ListSortDirection.Ascending)
                        result = result.ThenBy(sortingExp);
                    else
                        result = result.ThenByDescending(sortingExp);
                    sortedCount++;
                }
            }
           
            return result ?? (IOrderedQueryable<T>)source;
        }
    }
}
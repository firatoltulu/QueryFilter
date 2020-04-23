using System;
using System.Linq;
using System.Linq.Expressions;

namespace QueryFilter
{
    public static class ExpresionHelper
    {
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left, right), parameter);
        }

        public static EntityFilterModel<TEntity> ToEntityFilterModel<TEntity>(this QueryFilterModel queryFilter) where TEntity : class
        {
            Expression<Func<TEntity, bool>> expression = (e => true);

            if (queryFilter.FilterDescriptors.Count > 0)
                expression = expression.AndAlso(ExpressionMethodHelper.GetExpression<TEntity>(queryFilter.FilterDescriptors));

            EntityFilterModel<TEntity> entityQueryDto = new EntityFilterModel<TEntity>();
            entityQueryDto.Filter = expression;
            entityQueryDto.Skip = queryFilter.Skip;
            entityQueryDto.Take = queryFilter.Top;
            entityQueryDto.Sorts = string.Join(" ,", queryFilter.SortDescriptors.Select(sort => sort.ToString()));

            return entityQueryDto;
        }

        private class ReplaceExpressionVisitor
           : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }
    }
}
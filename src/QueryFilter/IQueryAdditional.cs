namespace QueryFilter
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Interface for adding additional query conditions to QueryFilter
    /// This allows for provider-specific query extensions for both LINQ to DB and Entity Framework
    /// </summary>
    /// <typeparam name="T">The entity type being queried</typeparam>
    public interface IQueryAdditional<T> where T : class
    {
        /// <summary>
        /// Applies additional query conditions to the IQueryable
        /// </summary>
        /// <param name="query">The source IQueryable to apply conditions to</param>
        /// <returns>The modified IQueryable with additional conditions</returns>
        IQueryable<T> Apply(IQueryable<T> query);

        /// <summary>
        /// Gets the expression that represents the additional condition
        /// </summary>
        /// <returns>An expression that can be combined with other filter expressions</returns>
        Expression<Func<T, bool>> GetExpression();
    }
}

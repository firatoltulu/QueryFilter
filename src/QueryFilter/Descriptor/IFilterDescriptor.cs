
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    using System.Linq.Expressions;
    /// <summary>
    /// Represents a filtering abstraction that knows how to create predicate filtering expression.
    /// </summary>
    public interface IFilterDescriptor
    {
        /// <summary>
        /// Creates a predicate filter expression used for collection filtering.
        /// </summary>
        /// <param name="instance">The instance expression, which will be used for filtering.</param>
        /// <returns>A predicate filter expression.</returns>
        Expression CreateFilterExpression(Expression instance);
    }
}
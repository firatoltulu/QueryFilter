// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    /// <summary>
    /// Logical operator used for filter descriptor composition.
    /// </summary>
    public enum FilterCompositionLogicalOperator
    {
        /// <summary>
        /// Combines filters with logical AND.
        /// </summary>
        And,

        /// <summary>
        /// Combines filters with logical OR.
        /// </summary>
        Or
    }
}
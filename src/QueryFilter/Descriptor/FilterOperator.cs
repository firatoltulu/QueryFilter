// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html
// All other rights reserved.
namespace QueryFilter
{
    /// <summary>
    /// Operator used in <see cref="FilterDescription"/>
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Left operand must be smaller than the right one.
        /// </summary>
        IsLessThan = 0,

        /// <summary>
        /// Left operand must be smaller than or equal to the right one.
        /// </summary>
        IsLessThanOrEqualTo = 1,

        /// <summary>
        /// Left operand must be equal to the right one.
        /// </summary>
        IsEqualTo = 2,

        /// <summary>
        /// Left operand must be different from the right one.
        /// </summary>
        IsNotEqualTo = 3,

        /// <summary>
        /// Left operand must be larger than the right one.
        /// </summary>
        IsGreaterThanOrEqualTo = 4,

        /// <summary>
        /// Left operand must be larger than or equal to the right one.
        /// </summary>
        IsGreaterThan = 5,

        /// <summary>
        /// Left operand must start with the right one.
        /// </summary>
        StartsWith = 6,

        /// <summary>
        /// Left operand must end with the right one.
        /// </summary>
        EndsWith = 7,

        /// <summary>
        /// Left operand must contain the right one.
        /// </summary>
        Contains = 8,

        /// <summary>
        /// Left operand must be contained in the right one.
        /// </summary>
        IsContainedIn = 9,

        /// <summary>
        /// Left operand must not be contained in the right one.
        /// </summary>
        NotContains = 10,

        Beetween = 11,

        Count = 12,

        NotStartsWith = 13,

        NotEndsWith = 14,

        NotIsContainedIn = 15,
    }
}

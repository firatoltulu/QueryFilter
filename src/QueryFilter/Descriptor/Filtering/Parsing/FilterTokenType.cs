
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    public enum FilterTokenType
    {
        Property,
        ComparisonOperator,
        Or,
        And,
        Not,
        Function,
        Number,
        String,
        Null,
        Empty,
        StringUseIgnoreCase,
        Boolean,
        DateTime,
        LeftParenthesis,
        RightParenthesis,
        LeftSquareBracket,
        RightSquareBracket,
        Comma,
        Time
    }
}

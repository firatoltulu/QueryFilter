
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    using System.Collections.Generic;
    
    public static class FilterTokenExtensions
    {
        private static readonly IDictionary<string, FilterOperator> tokenToOperator = new Dictionary<string, FilterOperator>
        {
            { "eq", FilterOperator.IsEqualTo },
            { "ne", FilterOperator.IsNotEqualTo },
            { "lt", FilterOperator.IsLessThan },
            { "le", FilterOperator.IsLessThanOrEqualTo },
            { "gt", FilterOperator.IsGreaterThan },
            { "ge", FilterOperator.IsGreaterThanOrEqualTo },
            { "startswith", FilterOperator.StartsWith },
            { "substringof", FilterOperator.Contains },
            { "endswith", FilterOperator.EndsWith },
            { "in", FilterOperator.IsContainedIn }
        };

        private static readonly IDictionary<FilterOperator, string> operatorToToken = new Dictionary<FilterOperator, string>
        {
            { FilterOperator.IsEqualTo, "eq" },
            { FilterOperator.IsNotEqualTo, "ne" },
            { FilterOperator.IsLessThan, "lt" },
            { FilterOperator.IsLessThanOrEqualTo, "le" },
            { FilterOperator.IsGreaterThan, "gt" },
            { FilterOperator.IsGreaterThanOrEqualTo, "ge" },
            { FilterOperator.StartsWith, "startswith" },
            { FilterOperator.Contains, "substringof" },
            { FilterOperator.NotContains, "nesubstringof" },
            { FilterOperator.EndsWith, "endswith" },
            { FilterOperator.IsContainedIn, "in" }
        };

        public static FilterOperator ToFilterOperator(this FilterToken token)
        {
            return tokenToOperator[token.Value];
        }

        public static string ToToken(this FilterOperator filterOperator)
        {
            return operatorToToken[filterOperator];
        }
    }
}

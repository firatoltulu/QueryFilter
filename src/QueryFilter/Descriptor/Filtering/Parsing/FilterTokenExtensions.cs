
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
            { "contains", FilterOperator.Contains },
            { "necontains", FilterOperator.NotContains },
            { "endswith", FilterOperator.EndsWith },
            { "in", FilterOperator.IsContainedIn },
            { "ct", FilterOperator.Count }

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
            { FilterOperator.Contains, "contains" },
            { FilterOperator.NotContains, "necontains" },
            { FilterOperator.EndsWith, "endswith" },
            { FilterOperator.IsContainedIn, "in" },
            { FilterOperator.Count, "ct" }

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

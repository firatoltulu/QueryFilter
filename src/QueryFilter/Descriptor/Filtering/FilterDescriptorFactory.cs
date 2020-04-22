
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

using System.Collections.Generic;

namespace QueryFilter
{
    public static class FilterDescriptorFactory
    {
        public static IList<IFilterDescriptor> Create(string input)
        {
            IList<IFilterDescriptor> result = new List<IFilterDescriptor>();

            FilterParser parser = new FilterParser(input);
            IFilterNode filterNode = parser.Parse();

            if (filterNode == null)
            {
                return result;
            }

            FilterNodeVisitor visitor = new FilterNodeVisitor();
            filterNode.Accept(visitor);
            result.Add(visitor.Result);
            return result;
        }

    }
}
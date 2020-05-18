
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    using System.Collections.Generic;
    using System.Linq;

    public class FilterNodeVisitor : IFilterNodeVisitor
    {
        private Stack<IFilterDescriptor> context;

        public FilterNodeVisitor()
        {
            context = new Stack<IFilterDescriptor>();
        }

        public IFilterDescriptor Result
        {
            get
            {
                return context.Pop();
            }
        }

        public IFilterDescriptor CurrentDescriptor
        {
            get
            {
                if (context.Count > 0)
                {
                    return context.Peek();
                }

                return null;
            }
        }

        public void StartVisit(IOperatorNode operatorNode)
        {
            FilterDescriptor filterDescriptor = new FilterDescriptor
            {
                Operator = operatorNode.FilterOperator
            };

            CompositeFilterDescriptor compositeFilterDescriptor = CurrentDescriptor as CompositeFilterDescriptor;

            if (compositeFilterDescriptor != null)
            {
                compositeFilterDescriptor.FilterDescriptors.Add(filterDescriptor);
            }

            context.Push(filterDescriptor);
        }

        public void StartVisit(ILogicalNode logicalNode)
        {
            CompositeFilterDescriptor filterDescriptor = new CompositeFilterDescriptor
            {
                LogicalOperator = logicalNode.LogicalOperator
            };

            CompositeFilterDescriptor compositeFilterDescriptor = CurrentDescriptor as CompositeFilterDescriptor;
            if (compositeFilterDescriptor != null)
            {
                compositeFilterDescriptor.FilterDescriptors.Add(filterDescriptor);
            }
            filterDescriptor.IsNested = logicalNode.IsNested;
            context.Push(filterDescriptor);
        }

        public void Visit(PropertyNode propertyNode)
        {
            ((FilterDescriptor)CurrentDescriptor).Member = propertyNode.Name;
        }

        public void EndVisit()
        {
            if (context.Count > 1)
            {
                context.Pop();
            }
        }

        public void Visit(IValueNode valueNode)
        {

            ((FilterDescriptor)CurrentDescriptor).Value = valueNode.Value;
        }

        public void VisitArray(IValueNode valueNode)
        {
            ((FilterDescriptor)CurrentDescriptor).Value = (valueNode.Value as object[]).Select(x => {
                IValueNode convertedValue = (IValueNode)x;
                return convertedValue.Value;
            }).ToArray();
        }
    }
}

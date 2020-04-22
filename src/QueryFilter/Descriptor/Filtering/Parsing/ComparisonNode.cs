
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    public class ComparisonNode : IFilterNode, IOperatorNode
    {
        public FilterOperator FilterOperator 
        { 
            get; 
            set; 
        }

        public virtual IFilterNode First 
        { 
            get; 
            set; 
        }

        public virtual IFilterNode Second
        {
            get;
            set;
        }

        public void Accept(IFilterNodeVisitor visitor)
        {
            visitor.StartVisit(this);
            First.Accept(visitor);
            Second.Accept(visitor);
            visitor.EndVisit();
        }


        public bool IsNested
        {
            get;
            set;
        }
    }
}

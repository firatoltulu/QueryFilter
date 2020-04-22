
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    using System.Collections.Generic;

    public class FunctionNode : IFilterNode, IOperatorNode
    {
        public FunctionNode()
        {
            Arguments = new List<IFilterNode>();
        }

        public FilterOperator FilterOperator
        {
            get;
            set;
        }

        public IList<IFilterNode> Arguments
        {
            get;
            private set;
        }

        public void Accept(IFilterNodeVisitor visitor)
        {
            visitor.StartVisit(this);

            foreach (IFilterNode argument in Arguments)
            {
                argument.Accept(visitor);
            }

            visitor.EndVisit();
        }

        public bool IsNested { get; set; }
    }
}

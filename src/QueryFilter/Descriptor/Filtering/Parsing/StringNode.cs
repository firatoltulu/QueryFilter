
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    public class StringNode : IFilterNode, IValueNode
    {
        public object Value
        {
            get;
            set;
        }

        public void Accept(IFilterNodeVisitor visitor)
        {
            ((FilterDescriptor)((FilterNodeVisitor)visitor).CurrentDescriptor).IgnoreCaseSensitive = this.IgnoreCaseSensitive;
            visitor.Visit(this);
        }

        public bool IgnoreCaseSensitive { get; set; }
    }
}

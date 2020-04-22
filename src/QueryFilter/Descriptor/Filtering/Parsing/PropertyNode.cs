
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    public class PropertyNode : IFilterNode
    {
        public string Name
        {
            get;
            set;
        }

        public void Accept(IFilterNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}

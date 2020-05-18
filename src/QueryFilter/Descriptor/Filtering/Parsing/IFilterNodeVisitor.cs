
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    public interface IFilterNodeVisitor
    {
        void Visit(PropertyNode propertyNode);

        void Visit(IValueNode valueNode);

        void StartVisit(ILogicalNode logicalNode);
        
        void StartVisit(IOperatorNode operatorNode);
        
        void EndVisit();
        void VisitArray(IValueNode valueNode);
    }
}

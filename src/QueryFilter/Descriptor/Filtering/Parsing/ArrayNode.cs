using System;
using System.Collections.Generic;
using System.Text;

namespace QueryFilter
{
    public class ArrayNode : IFilterNode, IValueNode, IArrayNode
    {
        public object Value
        {
            get;
            set;
        }

        public void Accept(IFilterNodeVisitor visitor)
        {
            visitor.VisitArray(this);
        }
    }
}

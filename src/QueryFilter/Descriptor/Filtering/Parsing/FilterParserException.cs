
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class FilterParserException : System.Exception
    {
        public FilterParserException()
        {
        }

        public FilterParserException(string message)
            : base(message)
        {
        }

        public FilterParserException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        protected FilterParserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

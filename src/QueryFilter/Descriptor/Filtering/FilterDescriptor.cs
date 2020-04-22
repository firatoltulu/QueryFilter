
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html
// All other rights reserved.

namespace QueryFilter
{
    using System;
    using System.Linq.Expressions;

    public partial class FilterDescriptor : FilterDescriptorBase
    {
        public FilterDescriptor()
            : this(string.Empty, FilterOperator.IsEqualTo, null)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FilterDescriptor(string member, FilterOperator filterOperator, object filterValue)
        {
            this.Member = member;
            this.Operator = filterOperator;
            this.Value = filterValue;
        }

        public object ConvertedValue
        {
            get
            {
                return this.Value;
            }
        }

        public string Member
        {
            get;
            set;
        }

        public Type MemberType { get; set; }

        public FilterOperator Operator
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }

        public bool IgnoreCaseSensitive { get; set; }

        protected override Expression CreateFilterExpression(ParameterExpression parameterExpression)
        {
            /*var builder = new FilterDescriptorExpressionBuilder(parameterExpression, this);
            builder.Options.CopyFrom(ExpressionBuilderOptions);

            return builder.CreateBodyExpression();*/
            return null;
        }

        public virtual bool Equals(FilterDescriptor other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                Equals(other.Operator, this.Operator) &&
                Equals(other.Member, this.Member) &&
                Equals(other.Value, this.Value);
        }

        public override bool Equals(object obj)
        {
            var other = obj as FilterDescriptor;
            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = this.Operator.GetHashCode();
                result = (result * 397) ^ (this.Member != null ? this.Member.GetHashCode() : 0);
                result = (result * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
                return result;
            }
        }
    }
}
namespace QueryFilter
{
    using System.ComponentModel;
    using System.Linq;
    /// <summary>
    /// Represents declarative sorting.
    /// </summary>
    public class GroupDescriptor : IDescriptor
    {
        public GroupDescriptor() { }
        public GroupDescriptor(string memberName)
        {
            Member = memberName;
        }

        /// <summary>
        /// Gets or sets the member name which will be used for sorting.
        /// </summary>
        /// <filterValue>The member that will be used for sorting.</filterValue>
        public string Member
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sort direction for this sort descriptor. If the value is null
        /// no sorting will be applied.
        /// </summary>
        /// <value>The sort direction. The default value is null.</value>

        public void Deserialize(string source)
        {
            Member = source;
        }

        public string Serialize()
        {
            return string.Format("{0}", Member);
        }

        public string ToLinq()
        {
            return $"Key.{Member}";
        }
    }
}

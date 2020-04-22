using System;
using System.ComponentModel;
using System.Linq;

namespace QueryFilter
{
    public enum AggType
    {
        Sum,
        Count,
        Min,
        Max
    }

    /// <summary>
    /// Represents declarative sorting.
    /// </summary>
    public class AggDescriptor : IDescriptor
    {
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
        public AggType AggType
        {
            get;
            set;
        }

        public void Deserialize(string source)
        {
            var parts = source.Split(new[] { '-' });

            if (parts.Length > 1)
            {
                Member = parts[0];
            }

            if (Member is null)
                throw new System.Exception("property can't be null.");


            AggType = (AggType)Enum.Parse(typeof(AggType), parts.Last(), true);
        }

        public string Serialize()
        {
            return string.Format("{0}-{1}", Member, AggType);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Member, AggType);
        }

        public string ToLinq()
        {
            return $"{AggType}({Member}) as {Member}";
        }

    }
}

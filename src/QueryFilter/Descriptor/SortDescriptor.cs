using System;
using System.ComponentModel;
using System.Linq;

namespace QueryFilter
{
    /// <summary>
    /// Represents declarative sorting.
    /// </summary>
    public class SortDescriptor : IDescriptor
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
        public ListSortDirection SortDirection
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

            if(Member is null)
                throw new System.Exception("property can't be null.");

            var sortDirection = parts.Last();

            SortDirection = sortDirection == "desc" ? ListSortDirection.Descending : ListSortDirection.Ascending;
        }

        public string Serialize()
        {
            return string.Format("{0}-{1}", Member, SortDirection == ListSortDirection.Ascending ? "asc" : "desc");
        }

        public string SerializeNoScore()
        {
            return string.Format("{0} {1}", Member, SortDirection == ListSortDirection.Ascending ? "asc" : "desc");
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", Member, SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC");
        }
    }
}

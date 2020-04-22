using System.Collections.Generic;

namespace QueryFilter
{
    public class PagedList<T>
    {
        public int TotalCount { get;  set; }
        public IEnumerable<T> Items { get;  set; }

        public PagedList(IEnumerable<T> source, int totalCount)
        {
            Items = source;
            TotalCount = totalCount;
        }
    }
}
namespace QueryFilter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;

    public class QueryFilterModel
    {
        public QueryFilterModel()
        {
            Top = 10;
            Skip = 0;
            SortDescriptors = new List<SortDescriptor>();
            GroupDescriptors = new List<GroupDescriptor>();
            AggDescriptors = new List<AggDescriptor>();
            SelectDescriptors = new List<SelectDescriptor>();
            FilterDescriptors = new List<IFilterDescriptor>();
        }

        public int Skip
        {
            get;
            set;
        }

        public int Top
        {
            get;
            set;
        }

        public IList<SortDescriptor> SortDescriptors
        {
            get;
            set;
        }

        public IList<IFilterDescriptor> FilterDescriptors
        {
            get;
            set;
        }

        public IList<SelectDescriptor> SelectDescriptors
        {
            get;
            set;
        }

        public IList<GroupDescriptor> GroupDescriptors
        {
            get;
            set;
        }

        public IList<AggDescriptor> AggDescriptors
        {
            get;
            set;
        }

        public static QueryFilterModel Parse(string queryFilter)
        {
            if (queryFilter.StartsWith("?"))
                queryFilter = queryFilter.Substring(1);

            var queries = Uri.UnescapeDataString(HttpUtility.UrlDecode(queryFilter)).Split('&').Where(o => o.StartsWith("$")).Select(dic =>
            {
                var _value = dic.Split('=');
                return new KeyValuePair<string, string>(_value[0], _value[1]);
            }).ToDictionary(x => x.Key, x => x.Value);

            var take = 10;
            var skip = 0;
            var filter = string.Empty;
            var select = string.Empty;
            var from = string.Empty;
            var orderby = string.Empty;
            var groupby = string.Empty;
            var agg = string.Empty;

            try
            {
                if (queries.ContainsKey("$top"))
                    take = int.Parse(queries["$top"]);

                if (queries.ContainsKey("$skip"))
                    skip = int.Parse(queries["$skip"]);

                if (queries.ContainsKey("$filter"))
                    filter = queries["$filter"].ToString();

                if (queries.ContainsKey("$select"))
                    select = queries["$select"].ToString();

                if (queries.ContainsKey("$orderby"))
                    orderby = queries["$orderby"].ToString();

                if (queries.ContainsKey("$from"))
                    from = queries["$from"].ToString();

                if (queries.ContainsKey("$groupby"))
                    groupby = queries["$groupby"].ToString();

                if (queries.ContainsKey("$aggby"))
                    agg = queries["$aggby"].ToString();
            }
            catch (Exception)
            {
                //handle exception
            }

            return Parse(from, skip, take, select, orderby, filter, groupby, agg);
        }

        public static QueryFilterModel Parse(int skip, int top, string orderBy, string filter, string select, string groupBy)
        {
            return Parse(string.Empty, skip, top, select, orderBy, filter, groupBy, null);
        }

        public static QueryFilterModel Parse(int skip, int top, string orderBy, string filter, string select)
        {
            return Parse(string.Empty, skip, top, select, orderBy, filter, null, null);
        }

        public static QueryFilterModel Parse(int skip, int top, string orderBy, string filter)
        {
            return Parse(string.Empty, skip, top, string.Empty, orderBy, filter, null, null);
        }

        public static QueryFilterModel Parse(string fromBy, int skip, int top)
        {
            return Parse(fromBy, skip, top, null, null, null, null, null);
        }

        public static QueryFilterModel Parse(string fromBy, int skip, int top, string select)
        {
            return Parse(fromBy, skip, top, select, null, null, null, null);
        }

        public static QueryFilterModel Parse(string fromBy, int skip, int top, string select, string orderBy)
        {
            return Parse(fromBy, skip, top, select, orderBy, null, null, null);
        }

        public static QueryFilterModel Parse(string fromBy, int skip, int top, string select, string orderBy, string filter, string groupBy, string agg)
        {
            QueryFilterModel result = new QueryFilterModel
            {
                Skip = skip,
                Top = top,
                From = fromBy,
                SortDescriptors = QueryFilterDescriptorSerializer.Deserialize<SortDescriptor>(orderBy),
                GroupDescriptors = QueryFilterDescriptorSerializer.Deserialize<GroupDescriptor>(groupBy),
                SelectDescriptors = QueryFilterDescriptorSerializer.Deserialize<SelectDescriptor>(select),
                AggDescriptors = QueryFilterDescriptorSerializer.Deserialize<AggDescriptor>(agg),
                FilterDescriptors = FilterDescriptorFactory.Create(filter)
            };
            return result;
        }

        public string From { get; set; }

        public System.Collections.Specialized.NameValueCollection Parameters { get; set; }

        private static string FindQueryKey(List<string> arr, Func<string, bool> find)
        {
            string result = string.Empty;
            var value = arr.FirstOrDefault(find);
            if (value != null)
                result = value.Split('=')[1];

            return result;
        }

        public object Clone()
        {
            var clone = this.MemberwiseClone() as QueryFilterModel;
            clone.Parameters = new System.Collections.Specialized.NameValueCollection(this.Parameters);
            clone.SelectDescriptors = this.SelectDescriptors.Select(s => new SelectDescriptor(s.Member)).ToList();
            clone.SortDescriptors = this.SortDescriptors.Select(s => new SortDescriptor() { Member = s.Member, SortDirection = s.SortDirection }).ToList();
            clone.GroupDescriptors = this.GroupDescriptors.Select(g => new GroupDescriptor(g.Member)).ToList();

            clone.FilterDescriptors = new List<IFilterDescriptor>();
            foreach (var filter in this.FilterDescriptors)
            {
                IFilterDescriptor cloneFilter = null;
                if (filter is CompositeFilterDescriptor)
                {
                    var compositeFilter = filter as CompositeFilterDescriptor;
                    cloneFilter = new CompositeFilterDescriptor()
                    {
                        IsNested = compositeFilter.IsNested,
                        LogicalOperator = compositeFilter.LogicalOperator,
                        FilterDescriptors = compositeFilter.FilterDescriptors
                    };
                }
                else
                {
                    var singleFilter = filter as FilterDescriptor;
                    cloneFilter = new FilterDescriptor(singleFilter.Member, singleFilter.Operator, singleFilter.Value);
                }

                clone.FilterDescriptors.Add(cloneFilter);
            }

            return clone;
        }

        private static NameValueCollection ToNameValueCollection<tValue>(IDictionary<string, tValue> dictionary)
        {
            var collection = new NameValueCollection();
            foreach (var pair in dictionary)
                collection.Add(pair.Key, pair.Value.ToString());
            return collection;
        }
    }
}
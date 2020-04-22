namespace QueryFilter
{
    public class FilterDescriptorJsonModel
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FilterDescriptorJsonModel(string member, FilterOperator filterOperator, object filterValue)
        {
            this.Member = member;
            this.Operator = filterOperator;
            this.Value = filterValue;
        }

        public string Member
        {
            get;
            set;
        }

        public string MemberType { get; set; }

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
    }
}
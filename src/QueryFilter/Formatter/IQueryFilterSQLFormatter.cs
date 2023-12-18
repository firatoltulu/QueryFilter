namespace QueryFilter.Formatter
{
    public interface IQueryFilterSQLFormatter
    {
        string Format(QueryFilterModel command);
        string FormatOnlyCount(QueryFilterModel command);
        string FormatOnlyFilter(QueryFilterModel command);
    }
}
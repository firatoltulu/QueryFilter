using System;

namespace QueryFilter.Formatter
{
    public class QueryFilterSQLFormatterProvider
    {
        public static IQueryFilterSQLFormatter GetDataProvider(QueryFilterSQLFormatterType queryFilterSQLFormatterType)
        {
            switch (queryFilterSQLFormatterType)
            {
                case QueryFilterSQLFormatterType.SqlServer:
                    return new SQLFormatter();

                case QueryFilterSQLFormatterType.Postgres:
                    return new PostgreSQLFormatter();

                default:
                    throw new ApplicationException($"Not supported data provider name: '{queryFilterSQLFormatterType}'");
            }
        }
    }
}
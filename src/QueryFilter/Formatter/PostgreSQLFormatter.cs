namespace QueryFilter.Formatter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json.Linq;

    public class PostgreSqlFormatter : IQueryFilterSQLFormatter
    {
        public PostgreSqlFormatter() => _builder = new StringBuilder();

        private readonly StringBuilder _builder;

        public string Format(QueryFilterModel command)
        {
            var formatter = new PostgreSqlFormatter();
            formatter.Select(command.SelectDescriptors);
            formatter.From(command.From);

            if (command.FilterDescriptors.Count > 0)
            {
                formatter.Where();
                formatter.Filter(command.FilterDescriptors);
            }
            formatter.Order(command.SortDescriptors);
            formatter.paged(command.Skip, command.Top);

            return formatter.ToString();
        }

        public string FormatOnlyCount(QueryFilterModel command)
        {
            var formatter = new PostgreSqlFormatter();
            formatter.Select(" Count(*) ");
            formatter.From(command.From);

            if (command.FilterDescriptors.Count > 0)
            {
                formatter.Where();
                formatter.Filter(command.FilterDescriptors);
            }
            return formatter.ToString();
        }

        public string FormatOnlyFilter(QueryFilterModel command)
        {
            var formatter = new PostgreSqlFormatter();

            formatter.Select(command.SelectDescriptors);
            From(command.From);
            if (command.FilterDescriptors.Count > 0)
            {
                formatter.Where();
                formatter.Filter(command.FilterDescriptors);
            }
            return formatter.ToString();
        }

        public override string ToString() => _builder.ToString();

        #region Write

        protected void Write(object value) => _builder.AppendFormat("{0}", value);

        protected void WriteWithSpace(object value) => _builder.AppendFormat(" {0} ", value);

        protected void WriteFormat(string value, params object[] args) => _builder.AppendFormat(value, args);

        protected virtual void WriteParameterName(string name) => Write("@" + name);

        protected virtual void WriteColumnName(string columnName) => Write(columnName);

        #endregion Write

        #region Formatter

        private void Select(IList<SelectDescriptor> selects)
        {
            Write(" SELECT ");
            if (selects.Count > 0)
            {
                var selectColumns = new List<string>();
                foreach (var select in selects)
                {
                    if (QueryFilterModel.Current?.JsonbColumns?.Contains(select.Member) == true)
                    {
                        // For JSON columns, use JSON extraction syntax
                        selectColumns.Add($"{select.Member}::text as {select.Member}");
                    }
                    else
                    {
                        selectColumns.Add(select.Member);
                    }
                }
                Write(string.Join(",", selectColumns));
            }
            else
            {
                Write(" * ");
            }
        }

        private void Select(string fields)
        {
            Write(" SELECT ");
            Write(fields);
        }

        private void From(string from) => WriteFormat(" FROM \"{0}\"  ", from);

        private void Where() => Write(" WHERE ");

        private void Filter(IList<IFilterDescriptor> filters)
        {
            if (filters.Count > 0)
            {
                foreach (var item in filters)
                {
                    Visit(item);
                    Write(" AND ");
                }

                if (_builder.ToString().EndsWith(" AND "))
                {
                    _builder.Remove(_builder.Length - 5, 5);
                }
            }
        }

        private void Order(IList<SortDescriptor> orders)
        {
            if (orders.Count > 0)
            {
                Write(" ORDER BY ");
                var orderBy = orders.Select(row => string.Format("\"{0}\" {1}", row.Member, row.SortDirection == System.ComponentModel.ListSortDirection.Ascending ? "asc" : "desc"));

                WriteFormat(string.Join(",", orderBy));
            }
        }

        private void paged(int skip, int top)
        {
            if (skip > -1)
            {
                WriteFormat(" OFFSET {0} ROWS ", skip);
                WriteFormat(" FETCH NEXT {0} ROWS ONLY ", top);
            }
        }

        #endregion Formatter

        #region prepaire

        private void Visit(IFilterDescriptor filter)
        {
            if (filter is CompositeFilterDescriptor)
            {
                var compositeFilter = filter as CompositeFilterDescriptor;
                if (compositeFilter.FilterDescriptors.Count > 0)
                {
                    if (compositeFilter.IsNested)
                    {
                        Write(" ( ");
                    }

                    for (int i = 0; i < compositeFilter.FilterDescriptors.Count; i++)
                    {
                        Visit(compositeFilter.FilterDescriptors[i]);

                        if (i < compositeFilter.FilterDescriptors.Count - 1)
                        {
                            switch (compositeFilter.LogicalOperator)
                            {
                                case FilterCompositionLogicalOperator.And:
                                    Write(" and ");
                                    break;

                                case FilterCompositionLogicalOperator.Or:
                                    Write(" or ");
                                    break;
                            }
                        }
                    }

                    if (compositeFilter.IsNested)
                    {
                        Write(" ) ");
                    }
                }
            }
            else
            {
                VisitBinary(filter as FilterDescriptor);
            }
        }

        private void VisitBinary(FilterDescriptor filter)
        {
            if (filter != null)
            {
                // Check if this is a JSON column or a nested JSON path
                bool isJsonColumn = false;
                string jsonColumn = null;
                string jsonPath = null;

                // Check if the member is a direct JSON column or a nested path
                if (filter.Member.Contains("."))
                {
                    // Handle nested JSON path (e.g., "UserFields.X")
                    var parts = filter.Member.Split(new[] { '.' }, 2);
                    if (QueryFilterModel.Current?.JsonbColumns?.Contains(parts[0]) == true)
                    {
                        isJsonColumn = true;
                        jsonColumn = parts[0];
                        jsonPath = parts[1];
                    }
                }
                else if (QueryFilterModel.Current?.JsonbColumns?.Contains(filter.Member) == true)
                {
                    // Direct JSON column
                    isJsonColumn = true;
                    jsonColumn = filter.Member;
                }

                if (isJsonColumn)
                {
                    // Handle JSON column filtering
                    Write(" ");

                    if (jsonPath != null)
                    {
                        // Check if this is an array column
                        bool isArrayColumn = QueryFilterModel.Current?.JsonbArrayColumns?.Contains(jsonColumn) == true;
                        
                        if (isArrayColumn)
                        {
                            // For array columns with nested paths, search for objects in array
                            switch (filter.Operator)
                            {
                                case FilterOperator.IsEqualTo:
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"::jsonb @> '[{");
                                    Write("\"");
                                    Write(jsonPath);
                                    Write("\":");
                                    WriteJsonValueForArray(filter.Value);
                                    Write("}]'::jsonb");
                                    break;
                                case FilterOperator.IsNotEqualTo:
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"::jsonb @> '[{");
                                    Write("\"");
                                    Write(jsonPath);
                                    Write("\":");
                                    WriteJsonValueForArray(filter.Value);
                                    Write("}]'::jsonb IS NOT TRUE");
                                    break;
                                case FilterOperator.Contains:
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"::jsonb @@ '$[*] ? (@.");
                                    Write(jsonPath);
                                    Write(" like_regex \".*");
                                    Write(filter.Value.ToString().Replace("'", "''").Replace("\\", "\\\\"));
                                    Write(".*\")'");
                                    break;
                                default:
                                    // For other operators, use jsonpath
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"::jsonb @@ '$[*] ? (@.");
                                    Write(jsonPath);
                                    Write(" ");
                                    Write(GetJsonPathOperator(filter.Operator));
                                    Write(" ");
                                    WriteValue(filter.Value);
                                    Write(")'");
                                    break;
                            }
                        }
                        else
                        {
                            // For nested JSON path in object columns
                            switch (filter.Operator)
                            {
                                case FilterOperator.IsEqualTo:
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"->>'");
                                    Write(jsonPath);
                                    Write("' = ");
                                    WriteValue(filter.Value);
                                    break;
                                case FilterOperator.IsNotEqualTo:
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"->>'");
                                    Write(jsonPath);
                                    Write("' != ");
                                    WriteValue(filter.Value);
                                    break;
                                case FilterOperator.Contains:
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"->>'");
                                    Write(jsonPath);
                                    Write("' LIKE ");
                                    Write("'%");
                                    Write(filter.Value);
                                    Write("%'");
                                    break;
                                default:
                                    // For other operators, use the standard text comparison
                                    Write("\"");
                                    Write(jsonColumn);
                                    Write("\"->>'");
                                    Write(jsonPath);
                                    Write("' ");
                                    Write(GetOperator(filter.Operator));
                                    WriteValue(filter.Value);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // For direct JSON column
                        Write("\"");
                        Write(jsonColumn);
                        Write("\"");
                        Write(" ");

                        // Check if this is an array column
                        bool isArrayColumn = QueryFilterModel.Current?.JsonbArrayColumns?.Contains(jsonColumn) == true;

                        switch (filter.Operator)
                        {
                            case FilterOperator.IsEqualTo:
                                if (isArrayColumn)
                                {
                                    // For array columns, wrap the value in an array
                                    Write("::jsonb @> ");
                                    WriteJsonArrayValue(filter.Value);
                                }
                                else
                                {
                                    Write("::jsonb @> ");
                                    WriteJsonValue(filter.Value);
                                }
                                break;
                            case FilterOperator.IsNotEqualTo:
                                if (isArrayColumn)
                                {
                                    Write("::jsonb @> ");
                                    WriteJsonArrayValue(filter.Value);
                                }
                                else
                                {
                                    Write("::jsonb @> ");
                                    WriteJsonValue(filter.Value);
                                }
                                Write(" IS NOT TRUE");
                                break;
                            case FilterOperator.Contains:
                                Write("::text LIKE ");
                                Write("'%");
                                Write(filter.Value);
                                Write("%'");
                                break;
                            case FilterOperator.IsContainedIn:
                                if (isArrayColumn && filter.Value.IsGenericList())
                                {
                                    // For array columns with list values, use the ?| operator (any of)
                                    Write("::jsonb ?| array");
                                    WriteJsonArray(filter.Value);
                                }
                                else
                                {
                                    Write("::jsonb <@ ");
                                    WriteJsonValue(filter.Value);
                                }
                                break;
                            case FilterOperator.NotIsContainedIn:
                                if (isArrayColumn && filter.Value.IsGenericList())
                                {
                                    // For array columns with list values, use the ?& operator (all of)
                                    Write("::jsonb ?& array");
                                    WriteJsonArray(filter.Value);
                                }
                                else
                                {
                                    Write("::jsonb <@ ");
                                    WriteJsonValue(filter.Value);
                                }
                                Write(" IS NOT TRUE");
                                break;
                            default:
                                // For other operators, fall back to text comparison
                                Write("::text ");
                                Write(GetOperator(filter.Operator));
                                WriteValue(filter.Value);
                                break;
                        }
                    }
                }
                else
                {
                    // Standard column handling
                    Write(" \"");
                    Write(filter.Member);
                    Write("\" ");

                    if (filter.Value == null)
                    {
                        if (filter.Operator == FilterOperator.IsEqualTo)
                        {
                            Write("IS NULL ");
                        }
                        else if (filter.Operator == FilterOperator.IsNotEqualTo)
                        {
                            Write("IS NOT NULL");
                        }
                    }
                    else if (string.IsNullOrEmpty(filter.Value.ToString()))
                    {
                        if (filter.Operator == FilterOperator.IsEqualTo)
                        {
                            Write(" = ''");
                        }
                        else if (filter.Operator == FilterOperator.IsNotEqualTo)
                        {
                            Write("!= ''");
                        }
                    }
                    else if (filter.Operator == FilterOperator.Contains ||
                          filter.Operator == FilterOperator.StartsWith ||
                          filter.Operator == FilterOperator.NotStartsWith ||
                          filter.Operator == FilterOperator.EndsWith ||
                          filter.Operator == FilterOperator.NotEndsWith)
                    {
                        var operatorFormat = GetOperator(filter.Operator);
                        Write(string.Format(operatorFormat, filter.Value));
                    }
                    else
                    {
                        Write(GetOperator(filter.Operator));
                        WriteValue(filter.Value);
                    }
                }
            }
        }

        private string GetOperator(FilterOperator b)
        {
            switch (b)
            {
                case FilterOperator.IsLessThan:
                    return "<";

                case FilterOperator.IsLessThanOrEqualTo:
                    return "<=";

                case FilterOperator.IsEqualTo:
                    return "=";

                case FilterOperator.IsNotEqualTo:
                    return "!=";

                case FilterOperator.IsGreaterThanOrEqualTo:
                    return ">=";

                case FilterOperator.IsGreaterThan:
                    return ">";

                case FilterOperator.StartsWith:
                    return "LIKE '{0}%'";

                case FilterOperator.NotStartsWith:
                    return "NOT LIKE '{0}%'";

                case FilterOperator.EndsWith:
                    return "LIKE '%{0}'";

                case FilterOperator.NotEndsWith:
                    return "NOT LIKE '%{0}'";

                case FilterOperator.Contains:
                    return "LIKE '%{0}%'";

                case FilterOperator.IsContainedIn:
                    return "IN ";

                case FilterOperator.NotIsContainedIn:
                    return "NOT IN ";
            }
            return "";
        }

        private string GetJsonPathOperator(FilterOperator b)
        {
            switch (b)
            {
                case FilterOperator.IsLessThan:
                    return "<";

                case FilterOperator.IsLessThanOrEqualTo:
                    return "<=";

                case FilterOperator.IsEqualTo:
                    return "==";

                case FilterOperator.IsNotEqualTo:
                    return "!=";

                case FilterOperator.IsGreaterThanOrEqualTo:
                    return ">=";

                case FilterOperator.IsGreaterThan:
                    return ">";

                default:
                    return "==";
            }
        }

        protected virtual void WriteValue(object value)
        {
            if (value == null)
            {
                Write("IS NULL");
            }
            else if (value.GetType().IsEnum)
            {
                Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));
            }
            else
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        Write(((bool)value) ? "true" : "false");
                        break;
                    case TypeCode.String:
                        Write("'");
                        Write(value);
                        Write("'");
                        break;

                    case TypeCode.Object:
                        if (value.IsGenericList() || value.GetType().IsArray)
                        {
                            var arrayLists = JArray.FromObject(value);
                            Write("(");

                            for (int i = 0; i < arrayLists.Count; i++)
                            {
                                var _row = (arrayLists[i] as JValue);
                                WriteValue(_row.Value.Convert(_row.Value.GetType()));

                                if (i < (arrayLists.Count - 1))
                                {
                                    Write(",");
                                }
                            }

                            Write(")");
                        }
                        else if (value is Guid)
                        {
                            Write("'");
                            Write(value.ToString());
                            Write("'");
                        }

                        break;

                    case TypeCode.Single:
                    case TypeCode.Double:
                        string str = value.ToString();
                        if (!str.Contains('.'))
                        {
                            str += ".0";
                        }
                        Write(str);
                        break;

                    case TypeCode.DateTime:
                        Write("'");
                        Write(Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        Write("'");
                        break;

                    default:
                        Write(value);
                        break;
                }
            }
        }

        protected virtual void WriteJsonValueForArray(object value)
        {
            // Similar to WriteJsonValue but without the ::jsonb suffix
            // Used when writing values inside JSON objects
            if (value == null)
            {
                Write("null");
            }
            else if (value.GetType().IsEnum)
            {
                Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));
            }
            else
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        Write(((bool)value) ? "true" : "false");
                        break;
                    case TypeCode.String:
                        Write("\"");
                        Write(value);
                        Write("\"");
                        break;
                    case TypeCode.Object:
                        if (value.IsGenericList() || value.GetType().IsArray)
                        {
                            // If the value is already an array, just serialize it directly
                            Write(Newtonsoft.Json.JsonConvert.SerializeObject(value));
                        }
                        else if (value is Guid)
                        {
                            Write("\"");
                            Write(value.ToString());
                            Write("\"");
                        }
                        else
                        {
                            // Serialize complex object to JSON
                            Write(Newtonsoft.Json.JsonConvert.SerializeObject(value));
                        }
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                        Write(value);
                        break;
                    case TypeCode.DateTime:
                        Write("\"");
                        Write(Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        Write("\"");
                        break;
                    default:
                        Write(value);
                        break;
                }
            }
        }

        protected virtual void WriteJsonValue(object value)
        {
            if (value == null)
            {
                Write("'null'::jsonb");
            }
            else if (value.GetType().IsEnum)
            {
                Write("'");
                Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));
                Write("'::jsonb");
            }
            else
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        Write("'");
                        Write(((bool)value) ? "true" : "false");
                        Write("'::jsonb");
                        break;
                    case TypeCode.String:
                        Write("'\"");
                        Write(value);
                        Write("\"'::jsonb");
                        break;
                    case TypeCode.Object:
                        if (value.IsGenericList() || value.GetType().IsArray)
                        {
                            var arrayLists = JArray.FromObject(value);
                            Write("'[");

                            for (int i = 0; i < arrayLists.Count; i++)
                            {
                                var _row = (arrayLists[i] as JValue);
                                var rowValue = _row.Value.Convert(_row.Value.GetType());

                                if (rowValue is string)
                                {
                                    Write("\"");
                                    Write(rowValue);
                                    Write("\"");
                                }
                                else
                                {
                                    Write(rowValue);
                                }

                                if (i < (arrayLists.Count - 1))
                                {
                                    Write(",");
                                }
                            }

                            Write("]'::jsonb");
                        }
                        else if (value is Guid)
                        {
                            Write("'\"");
                            Write(value.ToString());
                            Write("\"'::jsonb");
                        }
                        else
                        {
                            // Attempt to serialize complex object to JSON
                            Write("'");
                            Write(Newtonsoft.Json.JsonConvert.SerializeObject(value));
                            Write("'::jsonb");
                        }
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                        Write("'");
                        Write(value);
                        Write("'::jsonb");
                        break;
                    case TypeCode.DateTime:
                        Write("'\"");
                        Write(Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        Write("\"'::jsonb");
                        break;
                    default:
                        Write("'");
                        Write(value);
                        Write("'::jsonb");
                        break;
                }
            }
        }

        protected virtual void WriteJsonArrayValue(object value)
        {
            // Similar to WriteJsonValue but wraps the value in an array
            if (value == null)
            {
                Write("'");
                Write("[null]");
                Write("'::jsonb");
            }
            else if (value.GetType().IsEnum)
            {
                Write("'");
                Write("[");
                Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));
                Write("]");
                Write("'::jsonb");
            }
            else
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        Write("'");
                        Write("[");
                        Write(((bool)value) ? "true" : "false");
                        Write("]");
                        Write("'::jsonb");
                        break;
                    case TypeCode.String:
                        Write("'");
                        Write("[\"");
                        Write(value);
                        Write("\"]");
                        Write("'::jsonb");
                        break;
                    case TypeCode.Object:
                        if (value.IsGenericList() || value.GetType().IsArray)
                        {
                            // If the value is already an array, just serialize it directly
                            var arrayLists = JArray.FromObject(value);
                            Write("'");
                            Write(Newtonsoft.Json.JsonConvert.SerializeObject(value));
                            Write("'::jsonb");
                        }
                        else if (value is Guid)
                        {
                            Write("'");
                            Write("[\"");
                            Write(value.ToString());
                            Write("\"]");
                            Write("'::jsonb");
                        }
                        else
                        {
                            // Wrap complex object in array
                            Write("'");
                            Write("[");
                            Write(Newtonsoft.Json.JsonConvert.SerializeObject(value));
                            Write("]");
                            Write("'::jsonb");
                        }
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                        Write("'");
                        Write("[");
                        Write(value);
                        Write("]");
                        Write("'::jsonb");
                        break;
                    case TypeCode.DateTime:
                        Write("'");
                        Write("[\"");
                        Write(Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        Write("\"]");
                        Write("'::jsonb");
                        break;
                    default:
                        Write("'");
                        Write("[");
                        Write(value);
                        Write("]");
                        Write("'::jsonb");
                        break;
                }
            }
        }

        protected virtual void WriteJsonArray(object value)
        {
            // Write array for ?| and ?& operators
            if (value.IsGenericList() || value.GetType().IsArray)
            {
                var arrayLists = JArray.FromObject(value);
                Write("[");

                for (int i = 0; i < arrayLists.Count; i++)
                {
                    var _row = (arrayLists[i] as JValue);
                    var rowValue = _row.Value.Convert(_row.Value.GetType());

                    if (rowValue is string)
                    {
                        Write("'");
                        Write(rowValue);
                        Write("'");
                    }
                    else
                    {
                        Write(rowValue);
                    }

                    if (i < (arrayLists.Count - 1))
                    {
                        Write(",");
                    }
                }

                Write("]");
            }
        }

        #endregion prepaire
    }
}

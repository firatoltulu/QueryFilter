using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QueryFilter.Formatter
{
    public class SQLFormatter : IQueryFilterSQLFormatter
    {
        public SQLFormatter()
        {
            _builder = new StringBuilder();
        }

        private StringBuilder _builder = null;

        public string Format(QueryFilterModel command)
        {
            var formatter = new SQLFormatter();
            formatter.select(command.SelectDescriptors);
            formatter.from(command.From);

            if (command.FilterDescriptors.Count > 0)
            {
                formatter.where();
                formatter.filter(command.FilterDescriptors);
            }
            formatter.order(command.SortDescriptors);
            formatter.paged(command.Skip, command.Top);

            return formatter.ToString();
        }

        public string FormatOnlyCount(QueryFilterModel command)
        {
            var formatter = new SQLFormatter();
            formatter.select(" Count(*) ");
            formatter.from(command.From);

            if (command.FilterDescriptors.Count > 0)
            {
                formatter.where();
                formatter.filter(command.FilterDescriptors);
            }
            return formatter.ToString();
        }

        public string FormatOnlyFilter(QueryFilterModel command)
        {
            var formatter = new SQLFormatter();
            formatter.select(command.SelectDescriptors);
            formatter.from(command.From);
            if (command.FilterDescriptors.Count > 0)
            {
                formatter.where();
                formatter.filter(command.FilterDescriptors);
            }
            return formatter.ToString();
        }

        public override string ToString()
        {
            return this._builder.ToString();
        }

        #region Write

        protected void Write(object value)
        {
            this._builder.AppendFormat("{0}", value);
        }

        protected void WriteWithSpace(object value)
        {
            this._builder.AppendFormat(" {0} ", value);
        }

        protected void WriteFormat(string value, params object[] args)
        {
            this._builder.AppendFormat(value, args);
        }

        protected virtual void WriteParameterName(string name)
        {
            this.Write("@" + name);
        }

        protected virtual void WriteColumnName(string columnName)
        {
            this.Write(columnName);
        }

        #endregion Write

        #region Formatter

        private void select(IList<SelectDescriptor> selects)
        {
            Write(" SELECT ");
            if (selects.Count > 0)
                Write(string.Join(",", selects.Select(row => row.Member)));
            else
            {
                Write(" * ");
                //Write(" ,Count = COUNT(*) OVER() ");
            }
        }

        private void select(string fields)
        {
            Write(" SELECT ");
            Write(fields);
        }

        private void from(string from)
        {
            WriteFormat(" FROM {0} ", from);
        }

        private void where()
        {
            Write(" WHERE ");
        }

        private void filter(IList<IFilterDescriptor> filters)
        {
            if (filters.Count > 0)
            {
                foreach (var item in filters)
                {
                    Visit(item);
                }
            }
        }

        private void order(IList<SortDescriptor> orders)
        {
            if (orders.Count > 0)
            {
                Write(" ORDER BY ");
                var orderBy = orders.Select(row => string.Format("{0} {1}", row.Member, row.SortDirection == System.ComponentModel.ListSortDirection.Ascending ? "asc" : "desc"));
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

        private void Visit(IFilterDescriptor ex)
        {
            if (ex is CompositeFilterDescriptor)
            {
                var compositeFilter = ex as CompositeFilterDescriptor;

                if (compositeFilter.IsNested)
                    Write(" (");

                var left = compositeFilter.FilterDescriptors.FirstOrDefault();
                var right = compositeFilter.FilterDescriptors.LastOrDefault();
                Visit(left);
                Write(string.Format(" {0} ", compositeFilter.LogicalOperator.ToString().ToLower()));
                Visit(right);

                if (compositeFilter.IsNested)
                    Write(") ");
            }
            else if (ex is FilterDescriptor)
            {
                var filter = ex as FilterDescriptor;
                VisitBinary(filter);
            }
        }

        private void VisitBinary(IFilterDescriptor b)
        {
            var filter = b as FilterDescriptor;

            var op = GetOperator(filter.Operator);
            switch (filter.Operator)
            {
                case FilterOperator.IsLessThan:
                case FilterOperator.IsLessThanOrEqualTo:
                case FilterOperator.IsEqualTo:
                case FilterOperator.IsNotEqualTo:
                case FilterOperator.IsGreaterThanOrEqualTo:
                case FilterOperator.IsGreaterThan:
                    WriteWithSpace($"\"{filter.Member}\"");
                    WriteWithSpace(op);
                    WriteValue(filter.Value);
                    break;

                case FilterOperator.StartsWith:
                case FilterOperator.EndsWith:
                case FilterOperator.Contains:
                case FilterOperator.NotContains:
                case FilterOperator.NotEndsWith:
                case FilterOperator.NotStartsWith:

                    WriteWithSpace($"\"{filter.Member}\"");
                    WriteWithSpace(string.Format(op, filter.Value.ToString()));
                    break;

                case FilterOperator.NotIsContainedIn:
                case FilterOperator.IsContainedIn:
                    WriteWithSpace($"\"{filter.Member}\"");
                    WriteWithSpace(op);
                    WriteValue(filter.Value);
                    break;

                default:
                    break;
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

                case FilterOperator.NotContains:
                    return "LIKE '%{0}%'";

                case FilterOperator.IsContainedIn:
                    return "IN ";

                case FilterOperator.NotIsContainedIn:
                    return "NOT IN ";
            }
            return "";
        }

        protected virtual void WriteValue(object value)
        {
            if (value == null)
            {
                this.Write("NULL");
            }
            else if (value.GetType().IsEnum)
            {
                this.Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));
            }
            else
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        this.Write(((bool)value) ? 1 : 0);
                        break;

                    case TypeCode.String:
                        this.Write("'");
                        this.Write(value);
                        this.Write("'");
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
                                    Write(",");
                            }

                            Write(")");
                        }
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                        string str = value.ToString();
                        if (!str.Contains('.'))
                        {
                            str += ".0";
                        }
                        this.Write(str);
                        break;

                    case TypeCode.DateTime:
                        this.Write("N'");
                        this.Write(Convert.ToDateTime(value).ToString(new CultureInfo("en-US")));
                        this.Write("'");
                        break;

                    default:
                        this.Write(value);
                        break;
                }
            }
        }

        #endregion prepaire
    }
}
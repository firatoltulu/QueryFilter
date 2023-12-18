using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QueryFilter.Formatter
{
    public enum QueryFilterSQLFormatterType
    {
        /// <summary>
        /// Unknown
        /// </summary>
        [EnumMember(Value = "")]
        Unknown,

        /// <summary>
        /// MS SQL Server
        /// </summary>
        [EnumMember(Value = "sqlserver")]
        SqlServer,
 
        /// <summary>
        /// MySQL
        /// </summary>
        [EnumMember(Value = "postgres")]
        Postgres
    }
}

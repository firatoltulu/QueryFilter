namespace QueryFilter.Test.Samples
{
    using System;
    using System.Collections.Generic;
    using QueryFilter;
    using QueryFilter.Formatter;

    /// <summary>
    /// Examples of using JSON array columns with QueryFilter
    /// </summary>
    public class JsonArrayUsageSample
    {
        /// <summary>
        /// Example 1: Search for a specific value in a string array
        /// </summary>
        public static void SearchInStringArray()
        {
            var queryFilter = new QueryFilterModel();
            
            // Define that 'Tags' is a JSONB column containing an array
            queryFilter.JsonbColumns.Add("Tags");
            queryFilter.JsonbArrayColumns.Add("Tags");
            
            // Find documents where Tags array contains "technology"
            queryFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Tags",
                Operator = FilterOperator.IsEqualTo,
                Value = "technology"
            });
            
            var sql = new PostgreSqlFormatter().Format(queryFilter);
            // Generated SQL: WHERE "Tags" ::jsonb @> '["technology"]'::jsonb
            
            Console.WriteLine("String array search SQL:");
            Console.WriteLine(sql);
        }
        
        /// <summary>
        /// Example 2: Search for objects in an array by property
        /// </summary>
        public static void SearchObjectsInArray()
        {
            var queryFilter = new QueryFilterModel();
            
            // Define that 'Products' is a JSONB column containing an array of objects
            queryFilter.JsonbColumns.Add("Products");
            queryFilter.JsonbArrayColumns.Add("Products");
            
            // Find orders where Products array contains an object with category = "electronics"
            queryFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Products.category",
                Operator = FilterOperator.IsEqualTo,
                Value = "electronics"
            });
            
            var sql = new PostgreSqlFormatter().Format(queryFilter);
            // Generated SQL: WHERE "Products"::jsonb @> '[{"category":"electronics"}]'::jsonb
            
            Console.WriteLine("Object array search SQL:");
            Console.WriteLine(sql);
        }
        
        /// <summary>
        /// Example 3: Numeric comparison on array object properties
        /// </summary>
        public static void NumericComparisonInArray()
        {
            var queryFilter = new QueryFilterModel();
            
            queryFilter.JsonbColumns.Add("Items");
            queryFilter.JsonbArrayColumns.Add("Items");
            
            // Find items with price > 100 in the array
            queryFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Items.price",
                Operator = FilterOperator.IsGreaterThan,
                Value = 100
            });
            
            var sql = new PostgreSqlFormatter().Format(queryFilter);
            // Generated SQL: WHERE "Items"::jsonb @@ '$[*] ? (@.price > 100)'
            
            Console.WriteLine("Numeric comparison SQL:");
            Console.WriteLine(sql);
        }
        
        /// <summary>
        /// Example 4: Text search in array object properties
        /// </summary>
        public static void TextSearchInArray()
        {
            var queryFilter = new QueryFilterModel();
            
            queryFilter.JsonbColumns.Add("Users");
            queryFilter.JsonbArrayColumns.Add("Users");
            
            // Find users with email containing "@example.com"
            queryFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Users.email",
                Operator = FilterOperator.Contains,
                Value = "@example.com"
            });
            
            var sql = new PostgreSqlFormatter().Format(queryFilter);
            // Generated SQL: WHERE "Users"::jsonb @@ '$[*] ? (@.email like_regex ".*@example.com.*")'
            
            Console.WriteLine("Text search SQL:");
            Console.WriteLine(sql);
        }
        
        /// <summary>
        /// Example 5: Multiple conditions on array objects
        /// </summary>
        public static void MultipleConditionsOnArray()
        {
            var queryFilter = new QueryFilterModel();
            
            queryFilter.JsonbColumns.Add("Orders");
            queryFilter.JsonbArrayColumns.Add("Orders");
            
            // Find orders with status = "active" AND amount > 50
            var compositeFilter = new CompositeFilterDescriptor
            {
                LogicalOperator = FilterCompositionLogicalOperator.And
            };
            
            compositeFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Orders.status",
                Operator = FilterOperator.IsEqualTo,
                Value = "active"
            });
            
            compositeFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Orders.amount",
                Operator = FilterOperator.IsGreaterThan,
                Value = 50
            });
            
            queryFilter.FilterDescriptors.Add(compositeFilter);
            
            var sql = new PostgreSqlFormatter().Format(queryFilter);
            // Generated SQL: WHERE "Orders"::jsonb @> '[{"status":"active"}]'::jsonb and "Orders"::jsonb @@ '$[*] ? (@.amount > 50)'
            
            Console.WriteLine("Multiple conditions SQL:");
            Console.WriteLine(sql);
        }
        
        /// <summary>
        /// Example 6: Object column vs Array column difference
        /// </summary>
        public static void ObjectVsArrayColumn()
        {
            // Object column (not an array)
            var objectQuery = new QueryFilterModel();
            objectQuery.JsonbColumns.Add("Metadata");
            // Note: NOT adding to JsonbArrayColumns
            
            objectQuery.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata.title",
                Operator = FilterOperator.IsEqualTo,
                Value = "Example"
            });
            
            var objectSql = new PostgreSqlFormatter().Format(objectQuery);
            // Generated SQL: WHERE "Metadata"->>'title' = 'Example'
            
            Console.WriteLine("Object column SQL:");
            Console.WriteLine(objectSql);
            
            // Array column
            var arrayQuery = new QueryFilterModel();
            arrayQuery.JsonbColumns.Add("Tags");
            arrayQuery.JsonbArrayColumns.Add("Tags"); // Mark as array
            
            arrayQuery.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Tags",
                Operator = FilterOperator.IsEqualTo,
                Value = "example"
            });
            
            var arraySql = new PostgreSqlFormatter().Format(arrayQuery);
            // Generated SQL: WHERE "Tags" ::jsonb @> '["example"]'::jsonb
            
            Console.WriteLine("Array column SQL:");
            Console.WriteLine(arraySql);
        }
    }
}

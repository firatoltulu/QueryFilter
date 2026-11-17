namespace QueryFilter.Test
{
    using NUnit.Framework;
    using QueryFilter.Formatter;
    using System.Collections.Generic;

    [TestFixture]
    public class JsonColumnArrayTest
    {
        [TestCase]
        public void JsonColumn_ArrayContains_StringValue_Should_Produce_Correct_Query()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Tags");
            queryFilterModel.JsonbArrayColumns.Add("Tags");  // Mark as array column
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Tags",
                Operator = FilterOperator.IsEqualTo,
                Value = "technology"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert - Should produce array syntax for searching in array
            Assert.IsTrue(result.Contains("\"Tags\" ::jsonb @> '[\"technology\"]'::jsonb"), 
                $"Expected array syntax but got: {result}");
        }

        [TestCase]
        public void JsonColumn_ArrayContains_NumericValue_Should_Produce_Correct_Query()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Scores");
            queryFilterModel.JsonbArrayColumns.Add("Scores");  // Mark as array column
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Scores",
                Operator = FilterOperator.IsEqualTo,
                Value = 42
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert - Should produce array syntax for searching in array
            Assert.IsTrue(result.Contains("\"Scores\" ::jsonb @> '[42]'::jsonb"), 
                $"Expected array syntax but got: {result}");
        }

        [TestCase]
        public void JsonColumn_ArrayContains_BooleanValue_Should_Produce_Correct_Query()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Flags");
            queryFilterModel.JsonbArrayColumns.Add("Flags");  // Mark as array column
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Flags",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert - Should produce array syntax for searching in array
            Assert.IsTrue(result.Contains("\"Flags\" ::jsonb @> '[true]'::jsonb"), 
                $"Expected array syntax but got: {result}");
        }

        [TestCase]
        public void JsonColumn_ArrayContains_MultipleValues_Should_Produce_Correct_Query()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Categories");
            queryFilterModel.JsonbArrayColumns.Add("Categories");  // Mark as array column
            var values = new List<string> { "news", "tech", "science" };
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Categories",
                Operator = FilterOperator.IsContainedIn,
                Value = values
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert - Should check if column contains any of the values
            Assert.IsTrue(result.Contains("\"Categories\" ::jsonb ?| array['news','tech','science']"), 
                $"Expected ?| operator syntax but got: {result}");
        }
    }
}

namespace QueryFilter.Test
{
    using NUnit.Framework;
    using QueryFilter.Formatter;

    [TestFixture]
    public class DebugJsonPathTest
    {
        [TestCase]
        public void Debug_JsonPath_Output()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Products");
            queryFilterModel.JsonbArrayColumns.Add("Products");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Products.price",
                Operator = FilterOperator.IsGreaterThan,
                Value = 100
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            
            // Debug - print actual result
            System.Console.WriteLine("Actual SQL: " + result);
            
            // Check individual parts
            Assert.That(result, Does.Contain("Products"));
            Assert.That(result, Does.Contain("@@"));
            Assert.That(result, Does.Contain("$[*]"));
            Assert.That(result, Does.Contain("? (@.price > 100"));
        }
    }
}

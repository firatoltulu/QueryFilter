namespace QueryFilter.Test
{
    using NUnit.Framework;
    using QueryFilter.Formatter;

    [TestFixture]
    public class DebugArrayTest
    {
        [TestCase]
        public void Debug_Array_Query()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Tags");
            queryFilterModel.JsonbArrayColumns.Add("Tags");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Tags",
                Operator = FilterOperator.IsEqualTo,
                Value = "technology"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            
            // Debug - print actual result
            System.Console.WriteLine("Actual SQL: " + result);
            
            // Assert
            Assert.That(result, Contains.Substring("Tags"));
            Assert.That(result, Contains.Substring("@>"));
            Assert.That(result, Contains.Substring("technology"));
        }
    }
}

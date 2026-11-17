namespace QueryFilter.Test
{
    using NUnit.Framework;
    using QueryFilter.Formatter;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class JsonNestedPathTest
    {
        [TestCase]
        public void JsonColumn_NestedPath_EqualTo_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("UserFields");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.X",
                Operator = FilterOperator.IsEqualTo,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("\"UserFields\"->>'X' = 'value1'"));
        }

        [TestCase]
        public void JsonColumn_NestedPath_NotEqualTo_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("UserFields");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.X",
                Operator = FilterOperator.IsNotEqualTo,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("\"UserFields\"->>'X' != 'value1'"));
        }

        [TestCase]
        public void JsonColumn_NestedPath_Contains_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("UserFields");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.X",
                Operator = FilterOperator.Contains,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("\"UserFields\"->>'X' LIKE '%value1%'"));
        }

        [TestCase]
        public void JsonColumn_NestedPath_NumericValue_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("UserFields");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.Count",
                Operator = FilterOperator.IsGreaterThan,
                Value = 5
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("\"UserFields\"->>'Count' >5"));
        }

        [TestCase]
        public void JsonColumn_NestedPath_MultipleFilters_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("UserFields");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.X",
                Operator = FilterOperator.IsEqualTo,
                Value = "value1"
            });
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.Y",
                Operator = FilterOperator.IsEqualTo,
                Value = "value2"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("\"UserFields\"->>'X' = 'value1'"));
            Assert.IsTrue(result.Contains("\"UserFields\"->>'Y' = 'value2'"));
        }

        [TestCase]
        public void JsonColumn_MixedDirectAndNestedPath_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("UserFields");
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "UserFields.X",
                Operator = FilterOperator.IsEqualTo,
                Value = "value1"
            });
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = "value2"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("\"UserFields\"->>'X' = 'value1'"));
            Assert.IsTrue(result.Contains("\"Metadata\" ::jsonb @> '\"value2\"'::jsonb"));
        }
    }
}

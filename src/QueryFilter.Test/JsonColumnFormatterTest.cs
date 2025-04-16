namespace QueryFilter.Test
{
    using NUnit.Framework;
    using QueryFilter.Formatter;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class JsonColumnFormatterTest
    {
        [TestCase]
        public void JsonColumn_EqualTo_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '\"value1\"'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_NotEqualTo_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsNotEqualTo,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '\"value1\"'::jsonb IS NOT TRUE"));
        }

        [TestCase]
        public void JsonColumn_Contains_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.Contains,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::text LIKE '%value1%'"));
        }

        [TestCase]
        public void JsonColumn_IsContainedIn_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsContainedIn,
                Value = new List<string> { "value1", "value2" }
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb <@ '[\"value1\",\"value2\"]'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_NotIsContainedIn_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.NotIsContainedIn,
                Value = new List<string> { "value1", "value2" }
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb <@ '[\"value1\",\"value2\"]'::jsonb IS NOT TRUE"));
        }

        [TestCase]
        public void JsonColumn_ComplexObject_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = new { Name = "Test", Value = 123 }
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '{\"Name\":\"Test\",\"Value\":123}'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_NumericValue_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = 123
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '123'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_BooleanValue_Filter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> 'true'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_DateTimeValue_Filter_Success()
        {
            // Arrange
            var dateTime = new DateTime(2023, 1, 1, 12, 0, 0);
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = dateTime
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '\"2023-01-01 12:00:00.000\"'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_Select_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Metadata"));
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Name"));

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("SELECT Metadata::text as Metadata,Name"));
        }

        [TestCase]
        public void JsonColumn_MultipleJsonColumns_Select_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.JsonbColumns.Add("Properties");
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Metadata"));
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Properties"));
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Name"));

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("SELECT Metadata::text as Metadata,Properties::text as Properties,Name"));
        }

        [TestCase]
        public void JsonColumn_FilterAndSelect_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Metadata"));
            queryFilterModel.SelectDescriptors.Add(new SelectDescriptor("Name"));
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = "value1"
            });

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("SELECT Metadata::text as Metadata,Name"));
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '\"value1\"'::jsonb"));
        }

        [TestCase]
        public void JsonColumn_CompositeFilter_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.JsonbColumns.Add("Metadata");
            
            var compositeFilter = new CompositeFilterDescriptor
            {
                LogicalOperator = FilterCompositionLogicalOperator.And,
                IsNested = true
            };
            
            compositeFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Metadata",
                Operator = FilterOperator.IsEqualTo,
                Value = "value1"
            });
            
            compositeFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Name",
                Operator = FilterOperator.IsEqualTo,
                Value = "TestName"
            });
            
            queryFilterModel.FilterDescriptors.Add(compositeFilter);

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("(  Metadata ::jsonb @> '\"value1\"'::jsonb and"));
            Assert.IsTrue(result.Contains("\"Name\" ='TestName' )"));
        }

        [TestCase]
        public void JsonColumn_ParseFromString_Success()
        {
            // Arrange
            var queryString = "$filter=Metadata~eq~'value1'";
            var queryFilterModel = QueryFilterModel.Parse(queryString);
            queryFilterModel.JsonbColumns.Add("Metadata");

            // Act
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            // Assert
            Assert.IsTrue(result.Contains("Metadata ::jsonb @> '\"value1\"'::jsonb"));
        }
    }
}

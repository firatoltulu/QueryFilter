namespace QueryFilter.Test
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    [TestFixture]
    public class QueryAdditionalTest
    {
        private List<TestEntity> _testData;

        [SetUp]
        public void Setup()
        {
            // Initialize test data
            _testData = new List<TestEntity>
            {
                new TestEntity
                {
                    Id = 1,
                    Name = "Product 1",
                    IsActive = true,
                    Properties = new Dictionary<string, object>
                    {
                        { "Category", "Electronics" },
                        { "Price", 100.0 },
                        { "InStock", true }
                    }
                },
                new TestEntity
                {
                    Id = 2,
                    Name = "Product 2",
                    IsActive = true,
                    Properties = new Dictionary<string, object>
                    {
                        { "Category", "Clothing" },
                        { "Price", 50.0 },
                        { "InStock", true }
                    }
                },
                new TestEntity
                {
                    Id = 3,
                    Name = "Product 3",
                    IsActive = false,
                    Properties = new Dictionary<string, object>
                    {
                        { "Category", "Electronics" },
                        { "Price", 200.0 },
                        { "InStock", false }
                    }
                },
                new TestEntity
                {
                    Id = 4,
                    Name = "Product 4",
                    IsActive = true,
                    Properties = new Dictionary<string, object>
                    {
                        { "Category", "Books" },
                        { "Price", 25.0 },
                        { "InStock", true }
                    }
                },
                new TestEntity
                {
                    Id = 5,
                    Name = "Product 5",
                    IsActive = false,
                    Properties = new Dictionary<string, object>
                    {
                        { "Category", "Clothing" },
                        { "Price", 75.0 },
                        { "InStock", false }
                    }
                }
            };
        }

        [Test]
        public void StandardFilter_WithQueryAdditional_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "IsActive",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            });

            // Add a query additional that filters by a property in the dictionary
            queryFilterModel.AddQueryAdditional(new DictionaryPropertyQueryAdditional<TestEntity>(
                "Category", "Electronics"
            ));

            // Act
            var result = _testData.AsQueryable().ApplyFilter(queryFilterModel).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].Id);
            Assert.AreEqual("Electronics", result[0].Properties["Category"]);
        }

        [Test]
        public void MultipleQueryAdditionals_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();

            // Add standard filter
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "IsActive",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            });

            // Add multiple query additionals
            queryFilterModel.AddQueryAdditional(new DictionaryPropertyQueryAdditional<TestEntity>(
                "Price", 50.0, (value, propValue) => (double)propValue <= (double)value
            ));

            queryFilterModel.AddQueryAdditional(new DictionaryPropertyQueryAdditional<TestEntity>(
                "InStock", true
            ));

            // Act
            var result = _testData.AsQueryable().ApplyFilter(queryFilterModel).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(e => e.Id == 2));
            Assert.IsTrue(result.Any(e => e.Id == 4));
        }

        [Test]
        public void NumericComparison_QueryAdditional_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();

            // Add a query additional that filters by a numeric property in the dictionary
            queryFilterModel.AddQueryAdditional(new DictionaryPropertyQueryAdditional<TestEntity>(
                "Price", 100.0, (value, propValue) => (double)propValue > (double)value
            ));

            // Act
            var result = _testData.AsQueryable().ApplyFilter(queryFilterModel).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(3, result[0].Id);
            Assert.AreEqual(200.0, result[0].Properties["Price"]);
        }

        [Test]
        public void CombinedFilters_QueryAdditional_Success()
        {
            // Arrange
            var queryFilterModel = new QueryFilterModel();

            // Add standard filters
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Name",
                Operator = FilterOperator.Contains,
                Value = "Product"
            });

            // Add a composite filter descriptor
            var compositeFilter = new CompositeFilterDescriptor
            {
                LogicalOperator = FilterCompositionLogicalOperator.Or
            };

            compositeFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "IsActive",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            });

            compositeFilter.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Id",
                Operator = FilterOperator.IsGreaterThan,
                Value = 4
            });

            queryFilterModel.FilterDescriptors.Add(compositeFilter);

            // Add a query additional that filters by a property in the dictionary
            queryFilterModel.AddQueryAdditional(new DictionaryPropertyQueryAdditional<TestEntity>(
                "Category", "Clothing"
            ));

            // Act
            var result = _testData.AsQueryable().ApplyFilter(queryFilterModel).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(e => e.Id == 2));
            Assert.IsTrue(result.Any(e => e.Id == 5));
        }

        [Test]
        public void GetExpression_ReturnsValidExpression()
        {
            // Arrange
            var additional = new DictionaryPropertyQueryAdditional<TestEntity>("Category", "Electronics");

            // Act
            var expression = additional.GetExpression();
            var result = _testData.AsQueryable().Where(expression).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(e => e.Properties["Category"].ToString() == "Electronics"));
        }
    }

    /// <summary>
    /// Test entity class with a Dictionary property
    /// </summary>
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Implementation of IQueryAdditional that filters based on a property in a Dictionary
    /// </summary>
    public class DictionaryPropertyQueryAdditional<T> : IQueryAdditional<T> where T : class
    {
        private readonly string _propertyKey;
        private readonly object _propertyValue;
        private readonly Func<object, object, bool> _comparisonFunc;

        public DictionaryPropertyQueryAdditional(string propertyKey, object propertyValue,
            Func<object, object, bool> comparisonFunc = null)
        {
            _propertyKey = propertyKey;
            _propertyValue = propertyValue;
            _comparisonFunc = comparisonFunc ?? ((value, propValue) => propValue.Equals(value));
        }

        public IQueryable<T> Apply(IQueryable<T> query)
        {
            // Use the Where method with the expression
            return query.Where(GetExpression());
        }

        public Expression<Func<T, bool>> GetExpression()
        {
            // Create an expression without using pattern matching
            return entity => 
                typeof(TestEntity).IsAssignableFrom(entity.GetType()) && 
                ((TestEntity)(object)entity).Properties.ContainsKey(_propertyKey) && 
                _comparisonFunc(_propertyValue, ((TestEntity)(object)entity).Properties[_propertyKey]);
        }

        public override string ToString()
        {
            return $"Dictionary Property Filter: {_propertyKey} = {_propertyValue}";
        }
    }
}

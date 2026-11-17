namespace QueryFilter.Test.Samples
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Sample implementation of IQueryAdditional for Entity Framework
    /// </summary>
    public class EntityFrameworkQueryAdditional<T> : IQueryAdditional<T> where T : class
    {
        private readonly Expression<Func<T, bool>> _expression;
        private readonly string _description;

        public EntityFrameworkQueryAdditional(Expression<Func<T, bool>> expression, string description = null)
        {
            _expression = expression;
            _description = description ?? "Entity Framework additional condition";
        }

        public IQueryable<T> Apply(IQueryable<T> query)
        {
            // Apply the expression to the query
            return query.Where(_expression);
        }

        public Expression<Func<T, bool>> GetExpression()
        {
            return _expression;
        }

        public override string ToString()
        {
            return _description;
        }
    }

    /// <summary>
    /// Sample implementation of IQueryAdditional for LINQ to DB
    /// </summary>
    public class LinqToDbQueryAdditional<T> : IQueryAdditional<T> where T : class
    {
        private readonly Expression<Func<T, bool>> _expression;
        private readonly string _description;

        public LinqToDbQueryAdditional(Expression<Func<T, bool>> expression, string description = null)
        {
            _expression = expression;
            _description = description ?? "LINQ to DB additional condition";
        }

        public IQueryable<T> Apply(IQueryable<T> query)
        {
            // Apply the expression to the query
            // You can add LINQ to DB specific optimizations or extensions here
            return query.Where(_expression);
        }

        public Expression<Func<T, bool>> GetExpression()
        {
            return _expression;
        }

        public override string ToString()
        {
            return _description;
        }
    }

    /// <summary>
    /// Sample usage of QueryAdditional with different providers
    /// </summary>
    public static class QueryAdditionalUsageExamples
    {
        public static void EntityFrameworkExample<T>(IQueryable<T> dbSet) where T : class
        {
            // Create a query filter model
            var queryFilterModel = new QueryFilterModel();
            
            // Add standard filters
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "IsActive",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            });

            // Add Entity Framework specific conditions
            queryFilterModel.AddQueryAdditional(new EntityFrameworkQueryAdditional<T>(
                x => EF.Property<DateTime>(x, "LastModified") > DateTime.Now.AddDays(-30),
                "Recent items only (EF specific)"
            ));

            // Apply the filter to the query
            var result = dbSet.ApplyFilter(queryFilterModel);

            // Use the result
            // var items = result.ToList();
        }

        public static void LinqToDbExample<T>(IQueryable<T> table) where T : class
        {
            // Create a query filter model
            var queryFilterModel = new QueryFilterModel();
            
            // Add standard filters
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Status",
                Operator = FilterOperator.IsEqualTo,
                Value = "Active"
            });

            // Add LINQ to DB specific conditions
            queryFilterModel.AddQueryAdditional(new LinqToDbQueryAdditional<T>(
                x => Sql.Like(Sql.Property<string>(x, "Tags"), "%featured%"),
                "Featured items only (LINQ to DB specific)"
            ));

            // Apply the filter to the query
            var result = table.ApplyFilter(queryFilterModel);

            // Use the result
            // var items = result.ToList();
        }

        public static void CombinedExample<T>(IQueryable<T> query, bool isEntityFramework) where T : class
        {
            // Create a query filter model
            var queryFilterModel = new QueryFilterModel();
            
            // Add standard filters
            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "IsDeleted",
                Operator = FilterOperator.IsEqualTo,
                Value = false
            });

            // Add provider-specific conditions based on the current provider
            if (isEntityFramework)
            {
                queryFilterModel.AddQueryAdditional(new EntityFrameworkQueryAdditional<T>(
                    x => EF.Property<bool>(x, "IsSystemGenerated") == false,
                    "Non-system items only (EF specific)"
                ));
            }
            else
            {
                queryFilterModel.AddQueryAdditional(new LinqToDbQueryAdditional<T>(
                    x => Sql.Property<bool>(x, "IsSystemGenerated") == false,
                    "Non-system items only (LINQ to DB specific)"
                ));
            }

            // Apply the filter to the query
            var result = query.ApplyFilter(queryFilterModel);

            // Use the result
            // var items = result.ToList();
        }
    }

    // Mock classes for the examples
    public static class EF
    {
        public static TProperty Property<TProperty>(object entity, string propertyName)
        {
            // This is just a mock for the example
            return default;
        }
    }

    public static class Sql
    {
        public static TProperty Property<TProperty>(object entity, string propertyName)
        {
            // This is just a mock for the example
            return default;
        }

        public static bool Like(string value, string pattern)
        {
            // This is just a mock for the example
            return value?.Contains(pattern.Replace("%", "")) ?? false;
        }
    }
}

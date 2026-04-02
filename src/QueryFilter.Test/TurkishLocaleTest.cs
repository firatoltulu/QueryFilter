using NUnit.Framework;
using QueryFilter.Test.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace QueryFilter.Test
{
    [TestFixture]
    public class TurkishLocaleTest
    {
        private CultureInfo _originalCulture;

        private static readonly List<StudentModel> _students = new List<StudentModel>
        {
            new StudentModel { Name = "IDLE", LastName = "Test", Age = 25 },
            new StudentModel { Name = "Iron", LastName = "Man", Age = 30 },
            new StudentModel { Name = "Istanbul", LastName = "City", Age = 99 },
        };

        [SetUp]
        public void SetUp()
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
        }

        [TearDown]
        public void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
        }

        [Test]
        public void TurkishI_ToLower_DemonstratesProblem()
        {
            // Demonstrates why ToLowerInvariant is needed
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var turkishLower = "IDLE".ToLower();
            var invariantLower = "IDLE".ToLowerInvariant();

            Assert.AreNotEqual(turkishLower, invariantLower);
            Assert.AreEqual("ıdle", turkishLower, "Turkish ToLower produces dotless ı");
            Assert.AreEqual("idle", invariantLower, "InvariantCulture produces dotted i");
        }

        [Test]
        public void TurkishI_ConstantExpression_ShouldProduceInvariantLower()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var constant = Expression.Constant("IDLE");
            var trimToLowerExpr = constant.TrimToLower();

            var lambda = Expression.Lambda<Func<string>>(trimToLowerExpr);
            var result = lambda.Compile()();

            // After fix: should produce "idle" (invariant), not "ıdle" (Turkish)
            Assert.AreEqual("idle", result,
                "ConstantExpression.TrimToLower should use InvariantCulture");
        }

        [Test]
        public void TurkishI_UnaryExpression_ShouldProduceInvariantLower()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var convertedValue = Expression.Convert(Expression.Constant("IDLE"), typeof(string));
            var trimToLowerExpr = convertedValue.TrimToLower();

            var lambda = Expression.Lambda<Func<string>>(trimToLowerExpr);
            var result = lambda.Compile()();

            // After fix: should produce "idle" (invariant), not "ıdle" (Turkish)
            Assert.AreEqual("idle", result,
                "UnaryExpression.TrimToLower should use InvariantCulture");
        }

        [Test]
        public void TurkishI_Equal_ShouldMatch_WithTurkishCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'IDLE'");
            var result = _students.QueryFilter(queryFilterModel);

            Assert.AreEqual(1, result.TotalCount, "Should find 'IDLE' with eq operator under tr-TR");
        }

        [Test]
        public void TurkishI_Contains_ShouldMatch_WithTurkishCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~contains~'I'");
            var result = _students.QueryFilter(queryFilterModel);

            Assert.IsTrue(result.TotalCount >= 1, "Should find names containing 'I' under tr-TR");
        }

        [Test]
        public void TurkishI_StartsWith_ShouldMatch_WithTurkishCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~startswith~'I'");
            var result = _students.QueryFilter(queryFilterModel);

            Assert.IsTrue(result.TotalCount >= 1, "Should find names starting with 'I' under tr-TR");
        }

        [Test]
        public void TurkishI_InOperator_ShouldMatch_WithTurkishCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~in~['IDLE']");
            var result = _students.QueryFilter(queryFilterModel);

            Assert.AreEqual(1, result.TotalCount, "Should find 'IDLE' with in operator under tr-TR");
        }

        [Test]
        public void TurkishI_NotEqual_ShouldWork_WithTurkishCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~ne~'IDLE'");
            var result = _students.QueryFilter(queryFilterModel);

            Assert.AreEqual(2, result.TotalCount, "Should exclude 'IDLE' with ne operator under tr-TR");
        }

        [Test]
        public void InvariantCulture_Equal_ShouldAlwaysMatch()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'IDLE'");
            var result = _students.QueryFilter(queryFilterModel);

            Assert.AreEqual(1, result.TotalCount);
        }

        [Test]
        public void NullValue_TrimToLower_ShouldReturnConstant()
        {
            var constant = Expression.Constant(null, typeof(string));
            var result = constant.TrimToLower();

            Assert.IsInstanceOf<ConstantExpression>(result);
            Assert.IsNull(((ConstantExpression)result).Value);
        }
    }
}

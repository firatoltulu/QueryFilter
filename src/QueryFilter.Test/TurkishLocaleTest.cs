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
            new StudentModel { Name = "Nancy", LastName = "Test", Age = 25 },
            new StudentModel { Name = "Andrew", LastName = "Man", Age = 30 },
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
            // Demonstrates why ToLowerInvariant is needed for the constant side
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
            // The constant (parameter) side must use InvariantCulture
            // so that it matches the DB's LOWER() output
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var constant = Expression.Constant("IDLE");
            var trimToLowerExpr = constant.TrimToLower();

            var lambda = Expression.Lambda<Func<string>>(trimToLowerExpr);
            var result = lambda.Compile()();

            Assert.AreEqual("idle", result,
                "ConstantExpression.TrimToLower should use InvariantCulture to match DB LOWER()");
        }

        [Test]
        public void TurkishI_UnaryExpression_ShouldProduceInvariantLower()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            var convertedValue = Expression.Convert(Expression.Constant("IDLE"), typeof(string));
            var trimToLowerExpr = convertedValue.TrimToLower();

            var lambda = Expression.Lambda<Func<string>>(trimToLowerExpr);
            var result = lambda.Compile()();

            Assert.AreEqual("idle", result,
                "UnaryExpression.TrimToLower should use InvariantCulture to match DB LOWER()");
        }

        [Test]
        public void MemberExpression_ShouldUseToLower_ForORMCompatibility()
        {
            // MemberExpression must use ToLower (not ToLowerInvariant)
            // because ORM providers only translate ToLower() to SQL LOWER()
            var param = Expression.Parameter(typeof(StudentModel), "x");
            var member = Expression.Property(param, nameof(StudentModel.Name));
            var trimToLowerExpr = member.TrimToLower();

            // Verify it produces a MethodCallExpression calling ToLower
            var methodCall = trimToLowerExpr as MethodCallExpression;
            Assert.IsNotNull(methodCall);
            Assert.AreEqual("ToLower", methodCall.Method.Name,
                "MemberExpression.TrimToLower must use ToLower for ORM SQL translation");
        }

        [Test]
        public void InvariantCulture_Equal_ShouldMatch()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'");
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

        [Test]
        public void ConstantExpression_WithTurkishChars_ShouldNormalize()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            // Test various Turkish-sensitive characters
            var constant = Expression.Constant("ISTANBUL");
            var trimToLowerExpr = constant.TrimToLower();

            var lambda = Expression.Lambda<Func<string>>(trimToLowerExpr);
            var result = lambda.Compile()();

            // InvariantCulture: I -> i, not I -> ı
            Assert.AreEqual("istanbul", result,
                "Should produce 'istanbul' with invariant 'i', not Turkish 'ı'");
        }
    }
}

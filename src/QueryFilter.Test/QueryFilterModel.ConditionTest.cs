using NUnit.Framework;
using System;

namespace QueryFilter.Test
{
    public partial class QueryFilterModelTest
    {
        [TestCase("$filter=Age~eq~0")]
        [TestCase("$filter=Name~eq~'Fırat'")]
        public void DifferentTwoCaseParsing_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);

            Assert.AreEqual(queryFilterModel.FilterDescriptors.Count, 1);
        }

        [TestCase("$filter=Age~eq~1")]
        public void NumberConditionValueAndMemberParsing_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);

            var parsed = queryFilterModel.FilterDescriptors[0] as FilterDescriptor;

            Assert.AreEqual(parsed.Value, 1);
            Assert.AreEqual(parsed.Member, "Age");
        }

        [TestCase("$filter=NullMember~ne~null&$from=Products&$top=750")]
        public void NullMemberParsing_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);

            var parsed = queryFilterModel.FilterDescriptors[0] as FilterDescriptor;

            Assert.AreEqual(parsed.Value, null);
            Assert.AreEqual(parsed.Member, "NullMember");
        }

        [TestCase("$filter=Status~eq~'Success'~and~ExpireDate~lt~datetime'2020-06-10'")]
        public void DateMemberParsing_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);

            var parsed = queryFilterModel.FilterDescriptors[1] as FilterDescriptor;

            Assert.IsTrue(parsed.Value is DateTime);
            Assert.AreEqual(parsed.Member, "ExpireDate");
        }
    }
}
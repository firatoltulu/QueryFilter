using NUnit.Framework;
using System;
using System.Linq;

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

        [TestCase("$filter=Status~eq~'Success'~and~ExpireDate~lt~datetime'2020-06-17'")]
        public void DateMemberParsing_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);
            FilterDescriptor parsed;
            if (queryFilterModel.FilterDescriptors.FirstOrDefault() is CompositeFilterDescriptor)
            {
                var compositeQuery = (CompositeFilterDescriptor)queryFilterModel.FilterDescriptors.FirstOrDefault();
                parsed = (FilterDescriptor)compositeQuery.FilterDescriptors.LastOrDefault();
            }
            else
                parsed = (FilterDescriptor)queryFilterModel.FilterDescriptors.FirstOrDefault();

            Assert.IsTrue(parsed.Value is DateTime);
            Assert.AreEqual(parsed.Member, "ExpireDate");
        }

        [TestCase("$filter=Status~eq~'Success'~and~ExpireDate~lt~datetime'2020-11-05T05:49:13Z'")]
        public void UTCDateMemberParsing_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);
            FilterDescriptor parsed;
            if (queryFilterModel.FilterDescriptors.FirstOrDefault() is CompositeFilterDescriptor)
            {
                var compositeQuery = (CompositeFilterDescriptor)queryFilterModel.FilterDescriptors.FirstOrDefault();
                parsed = (FilterDescriptor)compositeQuery.FilterDescriptors.LastOrDefault();
            }
            else
                parsed = (FilterDescriptor)queryFilterModel.FilterDescriptors.FirstOrDefault();

            Assert.IsTrue(parsed.Value is DateTime);
            Assert.AreEqual(((DateTime)parsed.Value).Date, new DateTime(2020, 11, 05).Date);
            Assert.AreEqual(parsed.Member, "ExpireDate");
        }

       
    }
}
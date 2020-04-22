using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using QueryFilter;
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


    }
}

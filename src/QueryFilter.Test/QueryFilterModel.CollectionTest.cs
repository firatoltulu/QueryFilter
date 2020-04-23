using NUnit.Framework;
using QueryFilter.Test.Model;
using System;
using System.Collections.Generic;
using System.Text;
using QueryFilter;
using System.Linq;

namespace QueryFilter.Test
{

    [TestFixture]
    public partial class QueryFilterModelTest
    {
        private static readonly object[] _studentLists =
        {
            new object[] {
                new List<StudentModel> {
                new StudentModel { Name="Nancy",LastName="Fuller",Age=35 },
                new StudentModel { Name="Andrew",LastName="Leverling",Age=33 },
                new StudentModel { Name="Janet",LastName="Peacock",Age=32 }
               }}
        };

        [TestCaseSource("_studentLists")]
        public void StringMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.Items.FirstOrDefault().Name, "Nancy");
        }


        [TestCaseSource("_studentLists")]
        public void NumberMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Age~gt~32");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 2);
        }

    }
}

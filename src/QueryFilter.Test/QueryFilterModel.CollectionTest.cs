using NUnit.Framework;
using QueryFilter.Test.Model;
using System.Collections.Generic;
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
                new StudentModel { Name="Nancy",LastName="Fuller",Age=35, NullValue =1 },
                new StudentModel { Name="Andrew",LastName="Leverling",Age=33, NullValue=2 },
                new StudentModel { Name="Janet",LastName="Peacock",Age=32 , NullValue=null},
                new StudentModel { Name=string.Empty,LastName=string.Empty,Age=93,NullValue=3 }
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
            Assert.AreEqual(result.TotalCount, 3);
        }

        [TestCaseSource("_studentLists")]
        public void NumberMember_Filtered_Empty_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~null");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 1);
        }

        [TestCaseSource("_studentLists")]
        public void NumberMember_Filtered_Null_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=NullValue~eq~null");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 1);
        }

        [TestCaseSource("_studentLists")]
        public void NumberMember_FilteredIn_Not_Null_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Age~in~[32]");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 1);
        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredIn_Not_Null_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~in~['Nancy']");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 1);
        }
    }
}
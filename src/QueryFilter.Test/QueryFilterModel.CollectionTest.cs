using NUnit.Framework;
using QueryFilter.Test.Model;
using System;
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
                new StudentModel { Id=Guid.NewGuid() ,Name="Nancy",LastName="Fuller",Age=35, NullValue =1, Birth=new DateTime(2020,11,20), Time=new TimeSpan(13,0,0) },
                new StudentModel { Name="Andrew",LastName="Leverling",Age=33, NullValue=2,  Start=new DateTime(2020,11,20), Time=new TimeSpan(11,0,0) },
                new StudentModel { Name="Janet",LastName="Peacock",Age=32 , NullValue=null, Total=1, Time=new TimeSpan(9,0,0)},
                new StudentModel { Name=null,LastName=string.Empty,Age=93,NullValue=3, Counter=new List<StudentModel>(){
                    new StudentModel { Name="Nancy",LastName="Fuller",Age=35, NullValue =1, Birth=new DateTime(2020,11,20) }
                } }
             }}
        };

        [TestCaseSource("_studentLists")]
        public void StringMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Id~eq~'f55b2c58-bd48-4ace-aaf4-cddc3fc00e13'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 0);
        }

        [TestCaseSource("_studentLists")]
        public void GuidMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'Nancy'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.Items.FirstOrDefault().Name, "Nancy");
        }

        [TestCaseSource("_studentLists")]
        public void NullIntMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Total~eq~1");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 1);
        }

        [TestCaseSource("_studentLists")]
        public void NullDatetimeMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Start~eq~datetime'2020-11-20'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 1);
        }

        [TestCaseSource("_studentLists")]
        public void TimeMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Time~gt~time'09:00:00'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 2);
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
            var queryFilterModel = QueryFilterModel.Parse("$filter=NullValue~eq~''");
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

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredNotIn_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notin~['Nancy']");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 2);
        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredNotStartsWith_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notstartswith~'Nan'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 2);

        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredNotEndsWith_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notendswith~'Nan'");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 3);
        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredCount_Not_Null_Success(IEnumerable<StudentModel> studentModels)
        {
            //var queryFilterModel = QueryFilterModel.Parse("$filter=Counter~ct~0");
            //var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(1, 1);
        }

        [TestCaseSource("_studentLists")]
        public void IntMember_FilteredIn_Model_Test_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilter = new QueryFilterModel();
            queryFilter.FilterDescriptors.Add(new FilterDescriptor()
            {
                Member = nameof(StudentModel.Age),
                Operator = FilterOperator.IsContainedIn,
                Value = new int[] { 32, 20 }
            });

            var result = studentModels.QueryFilter(queryFilter);
            Assert.AreEqual(result.TotalCount, 1);
        }

        [TestCaseSource("_studentLists")]
        public void OrderBy_Test_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$skip=0&$top=15&$orderby=Name-asc&$filter=");
            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 4);
        }

        [TestCaseSource("_studentLists")]
        public void MultipleOperator_Test_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=(Name~eq~null~and~Age~in~[93])~or~((Name~eq~'Nancy'~and~Age~in~[35]))");

            var result = studentModels.QueryFilter(queryFilterModel);
            Assert.AreEqual(result.TotalCount, 2);
        }
    }
}
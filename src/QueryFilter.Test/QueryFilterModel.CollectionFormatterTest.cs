using NUnit.Framework;
using QueryFilter.Formatter;
using QueryFilter.Test.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryFilter.Test
{
    [TestFixture]
    public partial class CollectionFormatterTest
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
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            

            Assert.IsNotEmpty(result);
        }
 
        [TestCaseSource("_studentLists")]
        public void NullDatetimeMember_Filtered_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Start~eq~datetime'2020-11-20'");
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            Assert.IsNotEmpty(result);
        }

        
 

        [TestCaseSource("_studentLists")]
        public void NumberMember_FilteredIn_Not_Null_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Age~in~[32]");
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("32.0"));
        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredIn_Not_Null_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~in~['Nancy']");
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nancy"));
        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredNotIn_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notin~['Nancy']");
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nancy"));
        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredNotStartsWith_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notstartswith~'Nan'");
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nan"));

        }

        [TestCaseSource("_studentLists")]
        public void StringMember_FilteredNotEndsWith_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notendswith~'Nan'");
            var result = new PostgreSQLFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nan"));
        }

        

        [TestCaseSource("_studentLists")]
        public void IntMember_FilteredIn_Model_INT_Test_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilter = new QueryFilterModel();
            queryFilter.FilterDescriptors.Add(new FilterDescriptor()
            {
                Member = nameof(StudentModel.Age),
                Operator = FilterOperator.IsContainedIn,
                Value = new List<int>(){ 32, 20 }
            });

            var result = new PostgreSQLFormatter().Format(queryFilter);
            Assert.IsTrue(result.Contains("(32,20)"));

        }

        [TestCaseSource("_studentLists")]
        public void IntMember_FilteredIn_Model_GUID_Test_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilter = new QueryFilterModel();
            var input = Guid.NewGuid();
            queryFilter.FilterDescriptors.Add(new FilterDescriptor()
            {
                Member = nameof(StudentModel.Age),
                Operator = FilterOperator.IsContainedIn,
                Value = new List<Guid>() { input }
            });

            var result = new PostgreSQLFormatter().Format(queryFilter);
            Assert.IsTrue(result.Contains(input.ToString()));

        }


        [TestCaseSource("_studentLists")]
        public void MultipleOperator_Test_Success(IEnumerable<StudentModel> studentModels)
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=(Name~eq~null~and~Age~in~[93])~or~((Name~eq~'Nancy'~and~Age~in~[35]))");

            var result = new PostgreSQLFormatter().Format(queryFilterModel);

            Assert.IsTrue(result.Contains("(93.0)"));
            Assert.IsTrue(result.Contains("(35.0)"));
            Assert.IsTrue(result.Contains("Nancy"));


        }
    }
}
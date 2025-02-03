using NUnit.Framework;
using QueryFilter.Formatter;
using QueryFilter.Test.Model;
using System;
using System.Collections.Generic;

namespace QueryFilter.Test
{
    [TestFixture]
    public class CollectionFormatterTest
    {
        [TestCase]
        public void StringMember_Filtered_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Id~eq~'f55b2c58-bd48-4ace-aaf4-cddc3fc00e13'");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            Assert.IsNotEmpty(result);
        }

        [TestCase]
        public void NullDatetimeMember_Filtered_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Start~eq~datetime'2020-11-20'");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsNotEmpty(result);
        }

        [TestCase]
        public void NumberMember_FilteredIn_Not_Null_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Age~in~[32]");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("32.0"));
        }

        [TestCase]
        public void StringMember_FilteredIn_Not_Null_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~in~['Nancy']");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nancy"));
        }

        [TestCase]
        public void StringMember_FilteredNotIn_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notin~['Nancy']");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nancy"));
        }

        [TestCase]
        public void StringMember_FilteredNotStartsWith_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notstartswith~'Nan'");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nan"));
        }

        [TestCase]
        public void StringMember_FilteredNotEndsWith_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~notendswith~'Nan'");
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Contains("Nan"));
        }

        [TestCase]
        public void IntMember_FilteredIn_Model_INT_Test_Success()
        {
            var queryFilter = new QueryFilterModel();
            queryFilter.FilterDescriptors.Add(new FilterDescriptor()
            {
                Member = nameof(StudentModel.Age),
                Operator = FilterOperator.IsContainedIn,
                Value = new List<int>() { 32, 20 }
            });

            var result = new PostgreSqlFormatter().Format(queryFilter);
            Assert.IsTrue(result.Contains("(32,20)"));
        }

        [TestCase]
        public void IntMember_FilteredIn_Model_GUID_Test_Success()
        {
            var queryFilter = new QueryFilterModel();
            var input = Guid.NewGuid();
            queryFilter.FilterDescriptors.Add(new FilterDescriptor()
            {
                Member = nameof(StudentModel.Age),
                Operator = FilterOperator.IsContainedIn,
                Value = new List<Guid>() { input, input }
            });

            var result = new PostgreSqlFormatter().Format(queryFilter);
            Assert.IsTrue(result.Contains(input.ToString()));
        }

        [TestCase("$skip=0&$top=10&$orderby=CreatedOnUtc-desc&$filter=Categories.Id~in~['3ed1eec9-2be8-438b-9ea5-96432fdb4d5c','471a24dc-609e-48b9-8232-c6b201b1286a','57ead40f-08ea-4873-818e-49a178958dea']")]
        public void IntMember_FilteredIn_Model_FromString_Test_Success(string queryFilter)
        {
            var queryFilterModel = QueryFilterModel.Parse(queryFilter);
            var result = new PostgreSqlFormatter().Format(queryFilterModel);
            Assert.IsTrue(result.Trim().Equals(" SELECT  *  FROM \"\"   WHERE  \"Categories.Id\"  IN  ('3ed1eec9-2be8-438b-9ea5-96432fdb4d5c','471a24dc-609e-48b9-8232-c6b201b1286a','57ead40f-08ea-4873-818e-49a178958dea') ORDER BY \"CreatedOnUtc\" desc OFFSET 0 ROWS  FETCH NEXT 10 ROWS ONLY ".Trim()));
        }

        [TestCase]
        public void MultipleOperator_Test_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=(Name~eq~null~and~Age~in~[93])~or~((Name~eq~'Nancy'~and~Age~in~[35]))");

            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            Assert.IsTrue(result.Trim().Equals("SELECT  *  FROM \"\"   WHERE  ( \"Name\" IS NULL and  \"Age\"  IN  (93.0))  or  ( \"Name\"  = 'Nancy' and  \"Age\"  IN  (35.0))  OFFSET 0 ROWS  FETCH NEXT 10 ROWS ONLY".Trim()));
            Assert.IsTrue(result.Contains("(35.0)"));
            Assert.IsTrue(result.Contains("Nancy"));
        }

        [TestCase]
        public void Bool_Operator_Test_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=(IsActive~eq~true)");

            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            Assert.IsTrue(result.Trim().Equals(" SELECT  *  FROM \"\"   WHERE  \"IsActive\"  = true OFFSET 0 ROWS  FETCH NEXT 10 ROWS ONLY ".Trim()));
        }

        [TestCase]
        public void MultipleOperator_Test_Modified_Model_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=(Name~eq~null~and~Age~in~[93])~or~((Name~eq~'Nancy'~and~Age~in~[35]))");

            queryFilterModel.FilterDescriptors.Add(new FilterDescriptor
            {
                Member = "Name",
                Operator = FilterOperator.IsEqualTo,
                Value = "Selcuk"
            });

            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            Assert.AreEqual(result.Trim(), " SELECT  *  FROM \"\"   WHERE  ( \"Name\" IS NULL and  \"Age\"  IN  (93.0))  or  ( \"Name\"  = 'Nancy' and  \"Age\"  IN  (35.0))  AND  \"Name\"  = 'Selcuk' OFFSET 0 ROWS  FETCH NEXT 10 ROWS ONLY ".Trim());
        }

        [TestCase]
        public void NotEqual_Empty_Test_Modified_Model_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~'')");

            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            Assert.AreEqual(result.Trim(), " SELECT  *  FROM \"\"   WHERE  \"Name\" IS NULL OFFSET 0 ROWS  FETCH NEXT 10 ROWS ONLY ".Trim());
        }

        [TestCase]
        public void NotEqual_Empty_Keyword_Test_Modified_Model_Success()
        {
            var queryFilterModel = QueryFilterModel.Parse("$filter=Name~eq~empty)");

            var result = new PostgreSqlFormatter().Format(queryFilterModel);

            Assert.AreEqual(result.Trim(), " SELECT  *  FROM \"\"   WHERE  \"Name\"  = '' OFFSET 0 ROWS  FETCH NEXT 10 ROWS ONLY ".Trim());
        }
    }
}
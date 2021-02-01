using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using System.Linq;

namespace DG.XrmMockupTest
{
#if XRM_MOCKUP_TEST_2011
    [TestCaseOrderer("DG.XrmMockupTest.PriorityOrderer", "XrmMockup11Test")]
#endif
#if XRM_MOCKUP_TEST_2013
    [TestCaseOrderer("DG.XrmMockupTest.PriorityOrderer", "XrmMockup13Test")]
#endif
#if XRM_MOCKUP_TEST_2015
    [TestCaseOrderer("DG.XrmMockupTest.PriorityOrderer", "XrmMockup15Test")]
#endif
#if XRM_MOCKUP_TEST_2016
    [TestCaseOrderer("DG.XrmMockupTest.PriorityOrderer", "XrmMockup16Test")]
#endif
#if XRM_MOCKUP_TEST_365
    [TestCaseOrderer("DG.XrmMockupTest.PriorityOrderer", "XrmMockup365Test")]
#endif

    public class TestResetTables : UnitTestBaseNoReset
    {
        public TestResetTables(XrmMockupFixture fixture) : base(fixture) 
        {
            //create some contacts
            var contact1 = new Contact() { FirstName = "ResetTest1" };
            orgAdminService.Create(contact1);
            var contact2 = new Contact() { FirstName = "ResetTest2" };
            orgAdminService.Create(contact2);
        }

        [Fact]
        [TestPriority(1)]
        public void TestResetTable()
        {
            var account1 = new Account() { Name = "ResetTest1" };
            orgAdminService.Create(account1);
            var account2 = new Account() { Name = "ResetTest2" };
            orgAdminService.Create(account2);
            var account3 = new Account() { Name = "ResetTest3" };
            orgAdminService.Create(account3);

            var accountQuery = new QueryExpression("account");
            accountQuery.ColumnSet = new ColumnSet(true);
            var accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Equal(3, accounts.Entities.Where(x => x.Contains("name") && x.GetAttributeValue<string>("name").StartsWith("ResetTest")).Count());

            crm.ResetTable("account");

            accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Empty(accounts.Entities);

            account1 = new Account() { Name = "ResetTest1" };
            orgAdminService.Create(account1);
        }

        [Fact]
        [TestPriority(2)]
        public void TestAccountAndContactCounts()
        {
            var accountQuery = new QueryExpression("account");
            accountQuery.ColumnSet = new ColumnSet(true);
            var accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Single(accounts.Entities.Where(x => x.Contains("name") && x.GetAttributeValue<string>("name").StartsWith("ResetTest")));

            var contactQuery = new QueryExpression("contact");
            contactQuery.ColumnSet = new ColumnSet(true);
            var contacts = orgAdminService.RetrieveMultiple(contactQuery);
            Assert.Equal(4, contacts.Entities.Where(x => x.Contains("firstname") && x.GetAttributeValue<string>("firstname").StartsWith("ResetTest")).Count());
        }
    }

}

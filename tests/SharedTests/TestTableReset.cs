using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

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
            var contact1 = new Contact() { FirstName = "c1" };
            orgAdminService.Create(contact1);
            var contact2 = new Contact() { FirstName = "c2" };
            orgAdminService.Create(contact2);
        }

        [Fact]
        [TestPriority(1)]
        public void TestResetTable()
        {
            var account1 = new Account() { Name = "a1" };
            orgAdminService.Create(account1);
            var account2 = new Account() { Name = "a2" };
            orgAdminService.Create(account2);
            var account3 = new Account() { Name = "a3" };
            orgAdminService.Create(account3);

            var accountQuery = new QueryExpression("account");
            var accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Equal(3, accounts.Entities.Count);

            crm.ResetTable("account");

            accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Empty(accounts.Entities);

            account1 = new Account() { Name = "a1" };
            orgAdminService.Create(account1);
        }

        [Fact]
        [TestPriority(2)]
        public void TestAccountAndContactCounts()
        {
            var accountQuery = new QueryExpression("account");
            var accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Single(accounts.Entities);

            var contactQuery = new QueryExpression("contact");
            var contacts = orgAdminService.RetrieveMultiple(contactQuery);
            Assert.Equal(4, contacts.Entities.Count);
        }
    }

}

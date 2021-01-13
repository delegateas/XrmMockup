using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPlugins : UnitTestBase
    {
        public TestPlugins(XrmMockupFixture fixture) : base(fixture) { }

#if !XRM_MOCKUP_TEST_2011
        [Fact]
        public void TestImages()
        {
            // Testing that plugins not registered with DAXIF still have access to pre and post images during update.
            var createdAccount = new Account()
            {
                Name = "ChangeMePlease"
            };
            createdAccount.Id = orgAdminUIService.Create(createdAccount);

            orgAdminService.Update(createdAccount);
            var retrievedAccount = Account.Retrieve(orgAdminService, createdAccount.Id, x => x.Name);
            // The update plugin isn't run or the name it updates doesn't match what we are expecting!
            Assert.Equal("NameIsModified", retrievedAccount.Name);

            orgAdminUIService.Delete(Account.EntityLogicalName, createdAccount.Id);
        }
#endif

        [Fact]
        public void TestDirectIPluginImplementationPreOp()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "ChangeMePlease"
            };
            createdContact.Id = orgAdminUIService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.Equal("NameIsModified", retrievedContact.FirstName);

            orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
        }
        [Fact]
        public void TestDirectIPluginImplementationPostOp()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "ChangeMePleasePostOp"
            };
            createdContact.Id = orgAdminUIService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.Equal("NameIsModifiedPostOp", retrievedContact.FirstName);

            orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
        }

        [Fact]
        public void TestSystemAttributesAddedToTargetForPostOperationStepPlugins()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var con = new Contact
                {
                    FirstName = "CheckSystemAttributes"
                };
                con.Id = orgAdminUIService.Create(con);

                con = Contact.Retrieve(orgAdminService, con.Id, x => x.LastName, x => x.CreatedOn);
                Assert.True(!string.IsNullOrEmpty(con.LastName));
                Assert.Equal(con.CreatedOn.ToString(), con.LastName);
            }
        }

        [Fact]
        public void TestPluginTrigger()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                orgAdminUIService.Create(acc);

                var leads = context.LeadSet.ToList();
                Assert.True(leads.Count > 0);
            }
        }

        [Fact]
        public void TestPluginChain()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                var accid = orgAdminUIService.Create(acc);

                acc.Id = accid;
                acc.Fax = "1233213";

                Assert.Throws<FaultException>(() => orgAdminUIService.Update(acc));
            }
        }

#if !XRM_MOCKUP_TEST_2011
        [Fact]
        public void TestUpdateBase()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account
                {
                    Name = "Some"
                };
                acc.Id = orgAdminUIService.Create(acc);

                var accUpd = new Account(acc.Id)
                {
                    MarketCap = 20m
                };
                orgAdminUIService.Update(accUpd);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(acc.Name + "UpdateBase", retrieved.Name);
            }
        }
#endif

        [Fact]
        public void TestAddedFields()
        {
            var fax = new Entity("fax");
            fax.Id = orgAdminService.Create(fax);
            Assert.False(fax.Contains("category"));

            orgAdminService.Update(fax);
            Assert.False(fax.Contains("isbilled"));
        }

    }
}

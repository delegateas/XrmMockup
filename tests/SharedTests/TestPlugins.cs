using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestPlugins : UnitTestBase
    {
        public TestPlugins(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestImpersonatingUserIds()
        {
            var user1 = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, SecurityRoles.Salesperson); 
            var user2 = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, SecurityRoles.Salesperson);

            var user1Service = crm.CreateOrganizationService(user1.Id);

            //create a note as user 1;
            var note = new Entity("annotation");
            note["notetext"] = "test note";
            note.Id = user1Service.Create(note);

            var user2Service = crm.CreateOrganizationService(user2.Id);
            //edit note as user 2 - should not be allowed

            //create a note as user 1;
            var note2 = new Entity("annotation");
            note2["notetext"] = "test note2";
            note2.Id = user2Service.Create(note2);

           // var editNote2 = new Entity("annotation") { Id = note2.Id };
           // editNote2["notetext"] = note2.Id.ToString();
           // user1Service.Update(editNote2); //this shuold trigger the plugin running as admin to update note 2

            var editNote = new Entity("annotation") { Id = note.Id };
            editNote["notetext"] = note2.Id.ToString();
            user1Service.Update(editNote); //this shuold trigger the plugin running as admin to update note 2

            var checkNote = user2Service.Retrieve("annotation", note2.Id, new ColumnSet("notetext"));
            Assert.Equal("updated by admin plugin", checkNote.GetAttributeValue<string>("notetext"));
        }

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

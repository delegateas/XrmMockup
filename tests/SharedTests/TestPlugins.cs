using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestPlugins : UnitTestBase
    {
#if !XRM_MOCKUP_TEST_2011
        [TestMethod]
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
            Assert.AreEqual("NameIsModified", retrievedAccount.Name);

            orgAdminUIService.Delete(Account.EntityLogicalName, createdAccount.Id);
        }
#endif

        [TestMethod]
        public void TestImpersonatingUser()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "Matt Trinder"
            };
            createdContact.Id = salesUserService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.AreEqual("Matt Trinder", retrievedContact.FirstName, "The update plugin isn't run or the name it updates doesn't match what we are expecting!");

            var createdContact2 = new Contact()
            {
                FirstName = "Fred Bloggs"
            };
            createdContact2.Id = orgAdminService.Create(createdContact2);

            var retrievedContact2 = Contact.Retrieve(orgAdminService, createdContact2.Id, x => x.FirstName);
            Assert.AreEqual("Fred Bloggs", retrievedContact2.FirstName, "The update plugin isn't run or the name it updates doesn't match what we are expecting!");

            //check that the sales user can't delete the admin users contact
            try
            {
                salesUserService.Delete("contact", createdContact2.Id);
                Assert.Fail("should not be able to delete");
            }
            catch (System.Exception ex)
            {
                if (!ex.Message.Contains("delete"))
                {
                    throw;
                }
            }
            finally
            {
                //update the salesuser contact with the id of the admin contact as the first name
                //this will trigger the plugin running as admin to delete the admin contact.
                var updateContact = new Contact(createdContact.Id);
                updateContact.FirstName = createdContact2.Id.ToString();
                salesUserService.Update(updateContact);

                orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
            }
        }

        [TestMethod]
        public void TestDirectIPluginImplementationPreOp()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "ChangeMePlease"
            };
            createdContact.Id = orgAdminUIService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.AreEqual("NameIsModified", retrievedContact.FirstName, "The update plugin isn't run or the name it updates doesn't match what we are expecting!");

            orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
        }
        [TestMethod]
        public void TestDirectIPluginImplementationPostOp()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "ChangeMePleasePostOp"
            };
            createdContact.Id = orgAdminUIService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.AreEqual("NameIsModifiedPostOp", retrievedContact.FirstName, "The update plugin isn't run or the name it updates doesn't match what we are expecting!");

            orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
        }

        [TestMethod]
        public void TestSystemAttributesAddedToTargetForPostOperationStepPlugins()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var con = new Contact();
                con.FirstName = "CheckSystemAttributes";

                con.Id = orgAdminUIService.Create(con);

                con = Contact.Retrieve(orgAdminService, con.Id, x => x.LastName,x=>x.CreatedOn);
                Assert.IsTrue(!string.IsNullOrEmpty(con.LastName));
                Assert.AreEqual(con.CreatedOn.ToString(), con.LastName);
            }
        }

        [TestMethod]
        public void TestPluginTrigger()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                orgAdminUIService.Create(acc);

                var leads = context.LeadSet.ToList();
                Assert.IsTrue(leads.Count > 0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException))]
        public void TestPluginChain()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                var accid = orgAdminUIService.Create(acc);

                acc.Id = accid;
                acc.Fax = "1233213";

                orgAdminUIService.Update(acc);
            }
        }

#if !XRM_MOCKUP_TEST_2011
        [TestMethod]
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
                Assert.AreEqual(acc.Name + "UpdateBase", retrieved.Name);
            }
        }
#endif
    }
}

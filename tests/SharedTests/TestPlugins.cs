using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;

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
            Assert.AreEqual("NameIsModified", retrievedAccount.Name, "The update plugin isn't run or the name it updates doesn't match what we are expecting!");

            orgAdminUIService.Delete(Account.EntityLogicalName, createdAccount.Id);
        }
#endif
        [TestMethod]
        public void TestDirectIPluginImplementation()
        {
            // Testing that plugin which implement IPlugin directly are loaded and called as expected
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

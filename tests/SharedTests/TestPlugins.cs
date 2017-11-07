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

namespace DG.XrmMockupTest {
    [TestClass]
    public class TestPlugins : UnitTestBase {
        [TestMethod]
        public void TestImages() {
            using (var context = new Xrm(orgAdminUIService)) {

                var guid = orgAdminUIService.Create(new Account() { });

                orgAdminUIService.Delete(Account.EntityLogicalName, guid);


            }
        }

        [TestMethod]
        public void TestPluginTrigger() {
            using (var context = new Xrm(orgAdminUIService)) {
                var acc = new Account();
                orgAdminUIService.Create(acc);

                var leads = context.LeadSet.ToList();
                Assert.IsTrue(leads.Count > 0);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException))]
        public void TestPluginChain() {
            using (var context = new Xrm(orgAdminUIService)) {
                var acc = new Account();
                var accid = orgAdminUIService.Create(acc);

                acc.Id = accid;
                acc.Fax = "1233213";

                orgAdminUIService.Update(acc);
            }
        }


        [TestMethod]
        public void TestUpdateBase() {
            using (var context = new Xrm(orgAdminUIService)) {
                var acc = new Account {
                    Name = "Some"
                };
                acc.Id = orgAdminUIService.Create(acc);

                acc.MarketCap = 20m;
                orgAdminUIService.Update(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(acc.Name + "UpdateBase", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestSystemBase()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var contact = new Contact
                {
                    FirstName = "Some"
                };
                contact.Id = orgAdminUIService.Create(contact);
                
                var retrieved = orgAdminUIService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true)) as Contact;
                Assert.AreEqual("Test Name", retrieved.FirstName);
                Assert.AreEqual("Test Last Name", retrieved.LastName);
            }
        }
    }
}

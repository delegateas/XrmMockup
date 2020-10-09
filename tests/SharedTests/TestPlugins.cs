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

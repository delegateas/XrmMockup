using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestServiceRoles : UnitTestBase
    {
        [TestMethod]
        public void TestUpdateInactiveRecords()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminService.Create(account);

                account.StateCode = AccountState.Inactive;
                account.StatusCode = Account_StatusCode.Inactive;
                orgAdminService.Update(account);

                account.Name = "SDK can do";
                orgAdminService.Update(account);
                var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(account.Name, retrieved.Name);

                try
                {
                    orgAdminUIService.Update(account);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(MockupException));
                }
            }
        }


        [TestMethod, Ignore( "Disagreement over test")]
        // majakubowski: 
        // I don't agree with this test - I don't have experience with earlier version than 2015, 
        // but from this version all boolean attributes are set to default values also when created via SDK 
        public void TestCreateDefaultValues()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminService.Create(account);

                var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.IsNull(retrieved.DoNotPhone);

                var anotherAccount = new Account();
                anotherAccount.Id = orgAdminUIService.Create(anotherAccount);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, anotherAccount.Id, new ColumnSet(true)) as Account;
                Assert.IsTrue(retrieved.DoNotPhone.HasValue);
                Assert.IsFalse(retrieved.DoNotPhone.Value);
            }
        }

        [TestMethod]
        public void TestCreateCurrency()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminService.Create(account);
                var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.IsNull(retrieved.TransactionCurrencyId);

                account = new Account();
                account.Revenue = 20m;
                account.Id = orgAdminService.Create(account);
                retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.IsNotNull(retrieved.TransactionCurrencyId);

                account = new Account();
                account.Id = orgAdminUIService.Create(account);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.IsNotNull(retrieved.TransactionCurrencyId);
            }
        }
    }

}
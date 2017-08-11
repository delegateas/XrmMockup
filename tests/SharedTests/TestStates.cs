using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {
    [TestClass]
    public class TestStates : UnitTestBase {
        [TestMethod]
        public void TestSetState() {
            var acc = new Account() {
                StateCode = AccountState.Active,
                StatusCode = Account_StatusCode.Active
            };
            acc.Id = orgAdminUIService.Create(acc);
            acc.SetState(orgAdminUIService, AccountState.Inactive, Account_StatusCode.Inactive);

            var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
            Assert.AreEqual(AccountState.Inactive, retrieved.StateCode);
        }

#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
        [TestMethod]
        public void TestStatusTransitions()
        {
            var man = new dg_man() {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);
            man.SetState(orgAdminUIService, dg_manState.Inactive, dg_man_statuscode.Inactive);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;
            Assert.AreEqual(dg_manState.Inactive, retrieved.statecode);
            Assert.AreEqual(dg_man_statuscode.Inactive, retrieved.statuscode);

            try {
                man.SetState(orgAdminUIService, dg_manState.Active, dg_man_statuscode.Active);
                Assert.Fail();
            } catch(Exception e) {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }
#endif
    }
}

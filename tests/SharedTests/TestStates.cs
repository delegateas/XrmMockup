using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest {
    [TestClass]
    public class TestStates : UnitTestBase {
        [TestMethod]
        public void TestSetState() {
            var acc = new Account();
            acc.Id = orgAdminUIService.Create(acc);
            acc.SetState(orgAdminUIService, AccountState.Inactive);

            var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
            Assert.AreEqual(AccountState.Inactive, retrieved.StateCode);
        }
    }
}

using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestTime : UnitTestBase {
        [TestMethod]
        public void TestModifiedOn() {
            using (var context = new Xrm(orgAdminUIService)) {
                var offset = new TimeSpan(1, 0, 0, 0);
                var beforeCreate = DateTime.Now.Add(offset.Subtract(new TimeSpan(1)));
                crm.AddTime(offset);
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);
                
                var afterCreate = DateTime.Now.Add(offset.Add(new TimeSpan(1)));
                var service = crm.GetConfigurableAdminService(new MockupServiceSettings(true, true, MockupServiceSettings.Role.SDK));
                var retrieved = service.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.IsTrue(DateTime.Compare(beforeCreate, retrieved.CreatedOn.Value) < 0);
                Assert.IsTrue(DateTime.Compare(afterCreate, retrieved.CreatedOn.Value) > 0);

                var beforeUpdate = DateTime.Now.Add(offset.Subtract(new TimeSpan(1)));
                crm.AddTime(offset);
                acc.Name = "something";
                orgAdminUIService.Update(acc);
                var afterUpdate = DateTime.Now.Add(offset.Add(offset.Add(new TimeSpan(1))));
                retrieved = service.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.IsTrue(DateTime.Compare(beforeUpdate, retrieved.ModifiedOn.Value) < 0);
                Assert.IsTrue(DateTime.Compare(afterUpdate, retrieved.ModifiedOn.Value) > 0);
                Assert.IsTrue(DateTime.Compare(afterCreate, retrieved.CreatedOn.Value) > 0);
            }
        }
    }

}

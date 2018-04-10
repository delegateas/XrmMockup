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
    public class TestSettings : UnitTestBase {
        [TestMethod]
        public void TestNoExceptionRequest() {
            using (var context = new Xrm(orgAdminUIService)) {

                var req = new OrganizationRequest("WrongRequestThatFails");
                try {
                    orgAdminUIService.Execute(req);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(NotImplementedException));
                }

                req = new OrganizationRequest("TestWrongRequest");
                orgAdminUIService.Execute(req);
            }
        }

        [TestMethod,Ignore]
        public void TestRealDataRetrieve() {
            var acc = new Account(new Guid("9155CF31-BA6A-E611-80E0-C4346BAC0E68")) {
                Name = "babuasd"
            };
            orgRealDataService.Update(acc);
            var retrieved = orgRealDataService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.AreEqual(acc.Name, retrieved.Name);
            Assert.AreEqual("12321123312", retrieved.AccountNumber);
        }

        [TestMethod,Ignore]
        public void TestRealDataRetrieveMultiple() {
            var query = new QueryExpression(Account.EntityLogicalName) {
                ColumnSet = new ColumnSet(true),
                PageInfo = new PagingInfo() {
                    Count = 1000,
                    PageNumber = 1
                }
            };
            var res = orgRealDataService.RetrieveMultiple(query);
            Assert.IsTrue(res.Entities.Count > 0);
        }
    }
}

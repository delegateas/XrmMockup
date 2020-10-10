using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestSettings : UnitTestBase
    {
        [TestMethod]
        public void TestNoExceptionRequest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var req = new OrganizationRequest("WrongRequestThatFails");
                try
                {
                    orgAdminUIService.Execute(req);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(NotImplementedException));
                }

                req = new OrganizationRequest("TestWrongRequest");
                orgAdminUIService.Execute(req);
            }
        }

        [TestMethod, Ignore("Using real data")]
        public void TestRealDataRetrieve()
        {
            var acc = new Account(new Guid("9155CF31-BA6A-E611-80E0-C4346BAC0E68"))
            {
                Name = "babuasd"
            };
            orgRealDataService.Update(acc);
            var retrieved = orgRealDataService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.AreEqual(acc.Name, retrieved.Name);
            Assert.AreEqual("12321123312", retrieved.AccountNumber);
        }

        [TestMethod, Ignore( "Using real data")]
        public void TestRealDataRetrieveMultiple()
        {
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                PageInfo = new PagingInfo()
                {
                    Count = 1000,
                    PageNumber = 1
                }
            };
            var res = orgRealDataService.RetrieveMultiple(query);
            Assert.IsTrue(res.Entities.Count > 0);
        }
    }
}

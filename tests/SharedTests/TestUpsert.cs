#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Messages;
using DG.Some.Namespace;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{

    [TestClass]
    public class TestUpsert : UnitTestBase
    {

        [TestMethod]
        public void TestUpsertAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account1 = new Account { Name = "Account 1" };
                var account2 = new Account { Name = "Account 2" };
                
                var _account1id = orgAdminUIService.Create(account1);
                Assert.AreEqual(1, context.AccountSet.AsEnumerable().Where(
                    x => x.Name.StartsWith("Account")).Count());
                Assert.AreEqual("Account 1", context.AccountSet.First().Name);

                var bla = Account.Retrieve_dg_name(orgAdminUIService, "Account 2");

                context.ClearChanges();
                var req = new UpsertRequest();
                req.Target = new Account { Name = "New Account 1", Id = _account1id };
                var resp = orgAdminUIService.Execute(req) as UpsertResponse;
                Assert.IsFalse(resp.RecordCreated);
                Assert.AreEqual(1, context.AccountSet.AsEnumerable().Where(
                    x => x.Name.StartsWith("New Account")).Count());
                Assert.AreEqual("New Account 1", context.AccountSet.First().Name);

                context.ClearChanges();
                req.Target = account2;
                resp = orgAdminUIService.Execute(req) as UpsertResponse;
                Assert.IsTrue(resp.RecordCreated);
                Assert.AreEqual(2, context.AccountSet.AsEnumerable().Where(
                    x => x.Name.StartsWith("Account") || x.Name.StartsWith("New Account")).Count());
                

            }
        }
    }

}
#endif
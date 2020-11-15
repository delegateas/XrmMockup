#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using System.Linq;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestUpsert : UnitTestBase
    {
        [TestMethod]
        public void TestUpsertAll()
        {
            var guid = Guid.NewGuid().ToString();

            using (var context = new Xrm(orgAdminUIService))
            {
                var account1 = new Account { Name = $"{guid}Account 1" };
                var account2 = new Account { Name = $"{guid}Account 2" };

                var _account1id = orgAdminUIService.Create(account1);
                Assert.AreEqual(1,
                    context.AccountSet
                    .Where(x => x.Name.StartsWith($"{guid}Account"))
                    .ToList().Count
                );
                
                var bla = Account.Retrieve_dg_name(orgAdminUIService, $"{guid}Account 2");

                context.ClearChanges();
                var req = new UpsertRequest
                {
                    Target = new Account { Name = $"{guid}New Account 1", Id = _account1id }
                };
                var resp = orgAdminUIService.Execute(req) as UpsertResponse;
                Assert.IsFalse(resp.RecordCreated);
                Assert.AreEqual(1, 
                    context.AccountSet
                    .Where(x => x.Name.StartsWith($"{guid}New Account"))
                    .ToList().Count);
                
                context.ClearChanges();
                req.Target = account2;
                resp = orgAdminUIService.Execute(req) as UpsertResponse;
                Assert.IsTrue(resp.RecordCreated);
                Assert.AreEqual(2, context.AccountSet.AsEnumerable().Where(
                    x => (!string.IsNullOrEmpty(x.Name) &&( x.Name.StartsWith($"{guid}Account") || x.Name.StartsWith($"{guid}New Account")))).Count());


            }
        }
    }

}
#endif
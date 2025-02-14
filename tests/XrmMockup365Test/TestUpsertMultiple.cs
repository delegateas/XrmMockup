using System.Linq;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestUpsertMultiple : UnitTestBase
    {
        public TestUpsertMultiple(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestUpsertMultipleAll()
        {
            throw new System.NotImplementedException();
            //using (var context = new Xrm(orgAdminUIService))
            //{
            //    var account1 = new Account { Name = "Account 1" };
            //    var account2 = new Account { Name = "Account 2" };

            //    var _account1id = orgAdminUIService.Create(account1);
            //    Assert.Single(
            //         context.AccountSet
            //         .Where(x => x.Name.StartsWith("Account"))
            //         .ToList()
            //     );
            //    Assert.Equal("Account 1", context.AccountSet.First().Name);

            //    var bla = Account.Retrieve_dg_name(orgAdminUIService, "Account 2");

            //    context.ClearChanges();
            //    var req = new UpsertRequest
            //    {
            //        Target = new Account { Name = "New Account 1", Id = _account1id }
            //    };
            //    var resp = orgAdminUIService.Execute(req) as UpsertResponse;
            //    Assert.False(resp.RecordCreated);
            //    Assert.Single(
            //         context.AccountSet
            //         .Where(x => x.Name.StartsWith("New Account"))
            //         .ToList());
            //    Assert.Equal("New Account 1", context.AccountSet.First().Name);

            //    context.ClearChanges();
            //    req.Target = account2;
            //    resp = orgAdminUIService.Execute(req) as UpsertResponse;
            //    Assert.True(resp.RecordCreated);
            //    Assert.Equal(2, context.AccountSet.AsEnumerable().Where(
            //         x => x.Name.StartsWith("Account") || x.Name.StartsWith("New Account")).Count());


            //}
        }
    }

}

using System.Linq;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public class TestUpsertMultiple : UnitTestBase
    {
        public TestUpsertMultiple(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestUpsertMultipleAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account1 = new Account { Name = "Account 1" };

                var _account1id = orgAdminUIService.Create(account1);
                Assert.Single(
                     context.AccountSet
                     .Where(x => x.Name.StartsWith("Account"))
                     .ToList()
                 );
                Assert.Equal("Account 1", context.AccountSet.First().Name);

                var bla = Account.Retrieve_dg_name(orgAdminUIService, "Account 2");

                context.ClearChanges();

                var req = new UpsertMultipleRequest
                {
                    Targets = new EntityCollection
                    {
                        EntityName = Account.EntityLogicalName,
                        Entities = {
                            new Account(_account1id)
                            {
                                Name = "New Account 1"
                            },
                            new Account { Name = "Account 2" }
                        }
                    }
                };

                var resp = orgAdminUIService.Execute(req) as UpsertMultipleResponse;

                Assert.Collection(resp.Results,
                    r => Assert.False(r.RecordCreated),
                    r => Assert.True(r.RecordCreated)
                );

                Assert.Equal(2, context.AccountSet.AsEnumerable()
                    .Count(x => x.Name.StartsWith("Account") || x.Name.StartsWith("New Account")));

                Assert.Equal("New Account 1", context.AccountSet.First().Name);
                Assert.Equal("Account 2", context.AccountSet.Skip(1).First().Name);
            }
        }
    }
}

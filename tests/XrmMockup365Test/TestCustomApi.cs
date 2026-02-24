using DG.Some.Namespace;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System.Linq;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestCustomApi : UnitTestBase
    {
        public TestCustomApi(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateAccountApi()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_CreateAccount")
            {
                Parameters =
                {
                    { "Name", "TestAccount" },
                }
            });

            using (var context = new Xrm(orgAdminUIService))
            {
                var account = context.AccountSet.FirstOrDefault(x => x.Name == "TestAccount");
                Assert.NotNull(account);
                Assert.Equal(resp.Results["Name"] as string, account.Name);
            }
        }

        [Fact]
        public void TestLegacyCreateAccountApi()
        {
            var resp = orgAdminService.Execute(new OrganizationRequest("dg_" + nameof(LegacyCreateAccountApi))
            {
                Parameters =
                {
                    { "Name", "TestAccount" },
                }
            });

            using (var context = new Xrm(orgAdminUIService))
            {
                var account = context.AccountSet.FirstOrDefault(x => x.Name == "TestAccount");
                Assert.NotNull(account);
                Assert.Equal(resp.Results["Name"] as string, account.Name);
            }
        }
    }
}

using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;
using Xunit.Sdk;

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
    }
}

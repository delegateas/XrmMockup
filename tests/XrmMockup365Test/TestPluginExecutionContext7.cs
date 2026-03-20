using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestPluginExecutionContext7 : UnitTestBase
    {
        public TestPluginExecutionContext7(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestIPluginExecutionContext7IsResolvable()
        {
            // Creating an account triggers AccountPluginExecutionContext7PreOp,
            // which resolves IPluginExecutionContext7 and validates defaults.
            // If the plugin throws, this test fails.
            var account = new Account { Name = "Context7Test" };
            account.Id = orgAdminService.Create(account);

            Assert.NotEqual(System.Guid.Empty, account.Id);
        }
    }
}

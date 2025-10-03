using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Linq;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestDependencyInjection : UnitTestBase
    {
        public TestDependencyInjection(XrmMockupFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void CallsDependencyInjectedServiceInPlugin()
        {
            var accountId = orgAdminService.Create(new Account
            {
                Name = "Test Account for DI",
                AccountNumber = "DI-123"
            });

            using (var xrm = new Xrm(orgAdminService))
            {
                var contact = xrm.ContactSet
                    .Where(c => c.ParentCustomerId.Id == accountId)
                    .Select(c => c.ContactId)
                    .ToList();

                Assert.Single(contact);
            }
        }
    }
}

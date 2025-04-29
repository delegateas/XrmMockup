using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Linq;
using TestPluginAssembly365.Plugins.LegacyDaxif;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestExecutesLegacyDaxifPlugins : UnitTestBase
    {
        public TestExecutesLegacyDaxifPlugins(XrmMockupFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void TestExecutesLegacyDaxifPlugins_Create()
        {
            var account = new Account { Name = "TestAccount" };

            var id = orgAdminUIService.Create(account);

            var createdAccount = Account.Retrieve(orgAdminUIService, id);
            Assert.Equal("TestAccount", createdAccount.Name);

            // Check if the plugin executed
            using (var xrm = new Xrm(orgAdminService))
            {
                var createdLead = xrm.LeadSet.Single(l => l.ParentAccountId != null && l.ParentAccountId.Id == id && l.Subject.StartsWith(nameof(LegacyAccountPlugin)));
                Assert.StartsWith(nameof(LegacyAccountPlugin) + " Create: Some new lead ", createdLead.Subject);
            }
        }

        [Fact]
        public void TestExecutesLegacyDaxifPlugins_Update()
        {
            var account = new Account { Name = "TestAccount" };

            var id = orgAdminUIService.Create(account);
            account.Id = id;
            
            account.Name = "UpdatedAccount";
            orgAdminUIService.Update(account);

            var updatedAccount = Account.Retrieve(orgAdminUIService, id);
            Assert.Equal("UpdatedAccount", updatedAccount.Name);

            // Check if the plugin executed
            using (var xrm = new Xrm(orgAdminService))
            {
                var createdLead = xrm.LeadSet.Where(l => l.ParentAccountId != null && l.ParentAccountId.Id == id && l.Subject.StartsWith(nameof(LegacyAccountPlugin))).ToList();

                Assert.Collection(createdLead,
                    lead => Assert.StartsWith(nameof(LegacyAccountPlugin) + " Create: Some new lead ", lead.Subject),
                    lead => Assert.StartsWith(nameof(LegacyAccountPlugin) + " Update: Some new lead ", lead.Subject)
                );
            }
        }
    }
}

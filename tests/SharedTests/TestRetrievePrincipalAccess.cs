using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrievePrincipalAccess : UnitTestBase
    {
        public SystemUser TestUser;

        public TestRetrievePrincipalAccess(XrmMockupFixture fixture) : base(fixture) {
            TestUser = crm.CreateUser(orgAdminService, new SystemUser { BusinessUnitId = crm.RootBusinessUnit }, SecurityRoles._000TestingRole) as SystemUser;
        }

        [Fact]
        public void TestRetrievePrincipalAccessForTestUserOnLead()
        {
            var lead = new Lead();
            lead.Id = orgAdminService.Create(lead);

            var principalAccess = (RetrievePrincipalAccessResponse) orgAdminService.Execute(new RetrievePrincipalAccessRequest
            {
                Principal = TestUser.ToEntityReference(),
                Target = lead.ToEntityReference()
            });

            Assert.True(principalAccess.AccessRights == AccessRights.ReadAccess);
        }

        [Fact]
        public void TestRetrievePrincipalAccessForTestUserOnAccount()
        {
            var account = new Account();
            account.Id = orgAdminService.Create(account);

            var principalAccess = (RetrievePrincipalAccessResponse)orgAdminService.Execute(new RetrievePrincipalAccessRequest
            {
                Principal = TestUser.ToEntityReference(),
                Target = account.ToEntityReference()
            });

            Assert.True(principalAccess.AccessRights == (AccessRights.ReadAccess | AccessRights.CreateAccess));
        }
    }
}
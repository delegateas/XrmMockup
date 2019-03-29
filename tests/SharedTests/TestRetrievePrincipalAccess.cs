using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestRetrievePrincipalAccess : UnitTestBase
    {
        public SystemUser TestUser;

        [TestInitialize]
        public void CreateTestUser()
        {
            TestUser = crm.CreateUser(orgAdminService, new SystemUser { BusinessUnitId = crm.RootBusinessUnit }, SecurityRoles._000TestingRole) as SystemUser;
        }

        [TestMethod]
        public void TestRetrievePrincipalAccessForTestUserOnLead()
        {
            var lead = new Lead();
            lead.Id = orgAdminService.Create(lead);

            var principalAccess = (RetrievePrincipalAccessResponse) orgAdminService.Execute(new RetrievePrincipalAccessRequest
            {
                Principal = TestUser.ToEntityReference(),
                Target = lead.ToEntityReference()
            });

            Assert.IsTrue(principalAccess.AccessRights == AccessRights.ReadAccess);
        }

        [TestMethod]
        public void TestRetrievePrincipalAccessForTestUserOnAccount()
        {
            var account = new Account();
            account.Id = orgAdminService.Create(account);

            var principalAccess = (RetrievePrincipalAccessResponse)orgAdminService.Execute(new RetrievePrincipalAccessRequest
            {
                Principal = TestUser.ToEntityReference(),
                Target = account.ToEntityReference()
            });

            Assert.IsTrue(principalAccess.AccessRights == (AccessRights.ReadAccess | AccessRights.CreateAccess));
        }
    }
}
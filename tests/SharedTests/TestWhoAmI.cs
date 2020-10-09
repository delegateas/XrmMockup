using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using DG.XrmContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestWhoAmI : UnitTestBase
    {
        [TestMethod]
        public void WhoAmIRequestForAdmin()
        {
            var whoAmI = (WhoAmIResponse)orgAdminService.Execute(new WhoAmIRequest());
            var admin = orgAdminService.Retrieve<SystemUser>(crm.AdminUser.Id);
            Assert.AreEqual(whoAmI.UserId, admin.Id);
            Assert.AreEqual(whoAmI.BusinessUnitId, admin.BusinessUnitId.Id);
        }

        [TestMethod]
        public void WhoAmIRequestForOtherUser()
        {
            var bu = new BusinessUnit { Name = "child bu", ParentBusinessUnitId = crm.RootBusinessUnit };
            bu.Id = orgAdminUIService.Create(bu);
            var user = crm.CreateUser(orgAdminService, new SystemUser { BusinessUnitId = bu.ToEntityReference() }, SecurityRoles.SalesManager) as SystemUser;
            var service = crm.CreateOrganizationService(user.Id);
            var whoAmI = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
            Assert.AreEqual(whoAmI.UserId, user.Id);
            Assert.AreEqual(whoAmI.BusinessUnitId, bu.Id);
        }
    }
}
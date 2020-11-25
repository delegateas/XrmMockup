using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestSharing : UnitTestBase
    {
        [TestMethod]
        public void TestSharingAccess()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var otherUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles._000TestingRole);
                var sharingUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);

                var otherService = crm.CreateOrganizationService(otherUser.Id);
                var sharingService = crm.CreateOrganizationService(sharingUser.Id);
                var contact = new Contact();
                contact.Id = sharingService.Create(contact);

                try
                {
                    otherService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true));
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
                var req = new GrantAccessRequest
                {
                    PrincipalAccess = new PrincipalAccess()
                    {
                        AccessMask = AccessRights.ReadAccess,
                        Principal = otherUser.ToEntityReference()
                    },
                    Target = contact.ToEntityReference()
                };
                sharingService.Execute(req);

                otherService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true));
            }
        }
    }
}

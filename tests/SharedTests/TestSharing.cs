using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestSharing : UnitTestBase {
        [TestMethod]
        public void TestSharingAccess() {
            using (var context = new Xrm(orgAdminUIService)) {
                var businessunit = new BusinessUnit();
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var otherUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles._000TestingRole);
                var sharingUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);

                var otherService = crm.CreateOrganizationService(otherUser.Id);
                var sharingService = crm.CreateOrganizationService(sharingUser.Id);
                var contact = new Contact();
                contact.Id = sharingService.Create(contact);


                FaultException faultException = null;
                try {
                    otherService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true));
                    Assert.Fail();
                } catch (FaultException e) {
                    faultException = e;
                }
                var req = new GrantAccessRequest();
                req.PrincipalAccess = new PrincipalAccess() {
                    AccessMask = AccessRights.ReadAccess,
                    Principal = otherUser.ToEntityReference()
                };
                req.Target = contact.ToEntityReference();
                sharingService.Execute(req);
                
                otherService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true));
            }
        }
    }
}

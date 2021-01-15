using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest
{
    public class TestValidateUnchangedLookups : UnitTestBase
    {
        public TestValidateUnchangedLookups(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAppendTo()
        {
            var accountId = orgAdminService.Create(new Account() { Name = "testaccount" });
            var con = new Entity("contact");
            con["firstname"] = "assign";
            con["parentcustomerid"] = new EntityReference("account", accountId);
            var contactId = orgAdminService.Create(con);

            //now try and update the same fields again as min priv account

            var minUser = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "minuser@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;

            // Add account append privilege with basic level
            crm.AddPrivileges(
                minUser.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            var userService = crm.CreateOrganizationService(minUser.Id);

            con = new Entity("contact") { Id = contactId };
            con["firstname"] = "assign";
            con["parentcustomerid"] = new EntityReference("account", accountId);

            userService.Update(con);
        }

        [Fact]
        public void TestAssign()
        {
            var accountId = orgAdminService.Create(new Account() { Name = "testaccount" });
            var con = new Entity("contact");
            con["firstname"] = "assign";
            con["parentcustomerid"] = new EntityReference("account", accountId);
            var contactId = orgAdminService.Create(con);

            var contact = orgAdminService.Retrieve("contact", contactId, new ColumnSet(true)) as Contact;

            //now try and update the same fields again as min priv account

            var minUser = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "minuser@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;

            // Add account append privilege with basic level
            crm.AddPrivileges(
                minUser.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            var userService = crm.CreateOrganizationService(minUser.Id);

            con = new Entity("contact") { Id = contactId };
            con["firstname"] = "assign";
            con["parentcustomerid"] = new EntityReference("account", accountId);
            con["ownerid"] = new EntityReference("systemuser", contact.OwnerId.Id);

            userService.Update(con);
        }
    }
}

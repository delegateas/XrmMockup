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
            var acc = new Account() { Name = "testaccount" };
            acc.Id = orgAdminService.Create(acc);
            var con = new Contact()
            {
                FirstName = "assign",
                ParentCustomerId = acc.ToEntityReference()
            };
            con.Id = orgAdminService.Create(con);

            // Create user with write and append, but not append to
            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
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

            userService.Update(new Contact(con.Id)
            {
                FirstName = con.FirstName,
                ParentCustomerId = con.ParentCustomerId
            });
        }

        [Fact]
        public void TestAssign()
        {
            var acc = new Account() { Name = "testaccount" };
            acc.Id = orgAdminService.Create(acc);
            var con = new Contact()
            {
                FirstName = "assign"
            };
            con.Id = orgAdminService.Create(con);

            var contact = orgAdminService.Retrieve(con.LogicalName, con.Id, new ColumnSet(true)).ToEntity<Contact>();

            // Create user with write and append, but not assign
            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
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

            userService.Update(new Contact(con.Id)
            {
                FirstName = contact.FirstName,
                OwnerId = contact.OwnerId
            });
        }
    }
}

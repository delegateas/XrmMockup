using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestSecurityRoles : UnitTestBase
    {
#if !(XRM_MOCKUP_TEST_2011) //not sure why this fails for 2011 - possibly a different admin id for the roletemplate in the metadata...?
        
        [TestMethod]
        public void TestQueryOnLinkedRoleTemplate()
        {
            // All MS Dynamics CRM instances share the same System Admin role GUID.
            // Hence, we can hardode it as this will not represent a security issue
            Guid adminId = new Guid("627090FF-40A3-4053-8790-584EDC5BE201");

            var businessunit = new BusinessUnit();
            businessunit["name"] = "business unit name 1";
            businessunit.Id = orgAdminUIService.Create(businessunit);

            var user = new SystemUser();
            user["businessunitid"] = businessunit.ToEntityReference();
            var adminuser = crm.CreateUser(orgAdminService, user, SecurityRoles.SystemAdministrator);

            var q = new QueryExpression("role");
            q.Criteria.AddCondition("roletemplateid", ConditionOperator.Equal, adminId);
            var link = q.AddLink("systemuserroles", "roleid", "roleid");
            link.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, adminuser.Id);
            Assert.IsTrue( orgAdminService.RetrieveMultiple(q).Entities.Count > 0 );
        }
#endif

        [TestMethod]
        public void TestCreateSecurity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var orgUser = new SystemUser();
                orgUser.Id = orgAdminUIService.Create(orgUser);

                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 1";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var scheduler = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                var schedulerService = crm.CreateOrganizationService(scheduler.Id);

                try
                {
                    schedulerService.Create(new Lead());
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                schedulerService.Create(new Contact());

                try
                {
                    var contact = new Contact
                    {
                        OwnerId = orgUser.ToEntityReference()
                    };
                    schedulerService.Create(contact);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
            }
        }

        [TestMethod]
        public void TestAssignSecurityUser()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 2";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var contact = new Contact();
                contact.Id = service.Create(contact);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                service = crm.CreateOrganizationService(user.Id);

                var req = new AssignRequest();
                try
                {
                    req.Assignee = user.ToEntityReference();
                    req.Target = contact.ToEntityReference();
                    service.Execute(req);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
                var usersContact = new Contact();
                usersContact.Id = service.Create(usersContact);
                req.Assignee = orgUser.ToEntityReference();
                req.Target = usersContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [TestMethod]
        public void TestAssignSecurityTeam()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 3";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var contact = new Contact();
                contact.Id = service.Create(contact);

                var team = crm.CreateTeam(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Cannotreadcontact);

                var req = new AssignRequest();
                try
                {
                    req.Assignee = team.ToEntityReference();
                    req.Target = contact.ToEntityReference();
                    service.Execute(req);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
                var teamAdmin = crm.CreateTeam(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var adminContact = new Contact();
                adminContact.Id = service.Create(adminContact);
                req.Assignee = teamAdmin.ToEntityReference();
                req.Target = adminContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [TestMethod]
        public void TestUpdateSecurity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 4";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var bus = new dg_bus();
                bus.Id = service.Create(bus);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                service = crm.CreateOrganizationService(user.Id);

                try
                {
                    bus.dg_name = "new sadas";
                    service.Update(bus);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                var usersBus = new dg_bus();
                usersBus.Id = service.Create(usersBus);
                usersBus.dg_name = "sadsa";
                service.Update(usersBus);
            }
        }

        [TestMethod]
        public void TestDeleteSecurity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 5";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var bus = new dg_bus();
                bus.Id = service.Create(bus);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                service = crm.CreateOrganizationService(user.Id);

                try
                {
                    service.Delete(bus.LogicalName, bus.Id);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                var usersBus = new dg_bus();
                usersBus.Id = service.Create(usersBus);
                service.Delete(usersBus.LogicalName, usersBus.Id);
            }
        }

        [TestMethod]
        public void TestParentChangeCascading()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 6";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var parentUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                var service = crm.CreateOrganizationService(parentUser.Id);
                var bus = new dg_bus();
                bus.Id = service.Create(bus);

                var parentChild = new dg_child
                {
                    dg_parentBusId = bus.ToEntityReference()
                };
                parentChild.Id = service.Create(parentChild);

                var otherChild = new dg_child();
                otherChild.Id = orgAdminUIService.Create(otherChild);

                try
                {
                    service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                otherChild.dg_parentBusId = bus.ToEntityReference();
                orgAdminUIService.Update(otherChild);

                service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
            }
        }

    }

}

using System;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestSecurityRoles : UnitTestBase
    {
        public TestSecurityRoles(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                schedulerService.Create(new Contact());

                try
                {
                    var contact = new Contact
                    {
                        OwnerId = orgUser.ToEntityReference()
                    };
                    schedulerService.Create(contact);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
                var usersContact = new Contact();
                usersContact.Id = service.Create(usersContact);
                req.Assignee = orgUser.ToEntityReference();
                req.Target = usersContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
                var teamAdmin = crm.CreateTeam(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var adminContact = new Contact();
                adminContact.Id = service.Create(adminContact);
                req.Assignee = teamAdmin.ToEntityReference();
                req.Target = adminContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                var usersBus = new dg_bus();
                usersBus.Id = service.Create(usersBus);
                usersBus.dg_name = "sadsa";
                service.Update(usersBus);
            }
        }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                var usersBus = new dg_bus();
                usersBus.Id = service.Create(usersBus);
                service.Delete(usersBus.LogicalName, usersBus.Id);
            }
        }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                otherChild.dg_parentBusId = bus.ToEntityReference();
                orgAdminUIService.Update(otherChild);

                service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
            }
        }

    }

}

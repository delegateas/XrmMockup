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
    public class TestSecurityRoles : UnitTestBase {
        [TestMethod]
        public void TestCreateSecurity() {
            using (var context = new Xrm(orgAdminUIService)) {
                var orgUser = new SystemUser();
                orgUser.Id = orgAdminUIService.Create(orgUser);

                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 1";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var scheduler = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                var schedulerService = crm.CreateOrganizationService(scheduler.Id);

                try {
                    schedulerService.Create(new Lead());
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                schedulerService.Create(new Contact());

                try {
                    var contact = new Contact {
                        OwnerId = orgUser.ToEntityReference()
                    };
                    schedulerService.Create(contact);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
            }
        }

        [TestMethod]
        public void TestAssignSecurityUser() {
            using (var context = new Xrm(orgAdminUIService)) {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 2";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var contact = new Contact();
                contact.Id = service.Create(contact);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                service = crm.CreateOrganizationService(user.Id);
                FaultException faultException = null;
                var req = new AssignRequest();
                try {
                    req.Assignee = user.ToEntityReference();
                    req.Target = contact.ToEntityReference();
                    service.Execute(req);
                    Assert.Fail();
                } catch (FaultException e) {
                    faultException = e;
                }
                var usersContact = new Contact();
                usersContact.Id = service.Create(usersContact);
                req.Assignee = orgUser.ToEntityReference();
                req.Target = usersContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [TestMethod]
        public void TestAssignSecurityTeam() {
            using (var context = new Xrm(orgAdminUIService)) {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 3";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var contact = new Contact();
                contact.Id = service.Create(contact);

                var team = crm.CreateTeam(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Cannotreadcontact);
                FaultException faultException = null;
                var req = new AssignRequest();
                try {
                    req.Assignee = team.ToEntityReference();
                    req.Target = contact.ToEntityReference();
                    service.Execute(req);
                    Assert.Fail();
                } catch (FaultException e) {
                    faultException = e;
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
        public void TestUpdateSecurity() {
            using (var context = new Xrm(orgAdminUIService)) {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 4";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var bus = new dg_bus();
                bus.Id = service.Create(bus);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                service = crm.CreateOrganizationService(user.Id);

                FaultException faultException = null;
                try {
                    bus.dg_name = "new sadas";
                    service.Update(bus);
                    Assert.Fail();
                } catch (FaultException e) {
                    faultException = e;
                }

                var usersBus = new dg_bus();
                usersBus.Id = service.Create(usersBus);
                usersBus.dg_name = "sadsa";
                service.Update(usersBus);
            }
        }

        [TestMethod]
        public void TestDeleteSecurity() {
            using (var context = new Xrm(orgAdminUIService)) {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 5";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var bus = new dg_bus();
                bus.Id = service.Create(bus);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                service = crm.CreateOrganizationService(user.Id);
                FaultException faultException = null;
                try {
                    service.Delete(bus.LogicalName, bus.Id);
                    Assert.Fail();
                } catch (FaultException e) {
                    faultException = e;
                }

                var usersBus = new dg_bus();
                usersBus.Id = service.Create(usersBus);
                service.Delete(usersBus.LogicalName, usersBus.Id);
            }
        }

        [TestMethod]
        public void TestParentChangeCascading() {
            using (var context = new Xrm(orgAdminUIService)) {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 6";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var parentUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
                var service = crm.CreateOrganizationService(parentUser.Id);
                var bus = new dg_bus();
                bus.Id = service.Create(bus);

                var parentChild = new dg_child();
                parentChild.dg_parentBusId = bus.ToEntityReference();
                parentChild.Id = service.Create(parentChild);

                var otherChild = new dg_child();
                otherChild.Id = orgAdminUIService.Create(otherChild);
                FaultException faultException = null;
                try {
                    service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
                    Assert.Fail();
                } catch (FaultException e) {
                    faultException = e;
                }

                otherChild.dg_parentBusId = bus.ToEntityReference();
                orgAdminUIService.Update(otherChild);

                service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
            }
        }

    }

}

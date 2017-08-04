using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestCreate : UnitTestBase {
        [TestMethod]
        public void TestUserCreation() {
            using (var context = new Xrm(orgAdminUIService)) {

                var adminUser = orgAdminUIService.Retrieve("systemuser", crm.AdminUser.Id, new ColumnSet("businessunitid"));
                var adminBusinessunitid = adminUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                var adminBusinessunit = orgAdminUIService.Retrieve("businessunit", adminBusinessunitid, new ColumnSet("name"));


                var user = new Entity("systemuser");
                user.Attributes["firstname"] = "Test User";
                var userid = orgAdminUIService.Create(user);

                var retrievedUser = orgAdminUIService.Retrieve("systemuser", userid, new ColumnSet(true));
                Assert.AreEqual(user.Attributes["firstname"], retrievedUser.Attributes["firstname"]);
                var businessunitid = retrievedUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                var businessunit = orgAdminUIService.Retrieve("businessunit", businessunitid, new ColumnSet("name"));

                Assert.AreEqual(adminBusinessunit.Attributes["name"], businessunit.Attributes["name"]);
            }
        }

        [TestMethod]
        public void TestTeamCreation() {
            using (var context = new Xrm(orgAdminUIService)) {

                var adminUser = orgAdminUIService.Retrieve("systemuser", crm.AdminUser.Id, new ColumnSet("businessunitid"));
                var adminBusinessunitid = adminUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                var adminBusinessunit = orgAdminUIService.Retrieve("businessunit", adminBusinessunitid, new ColumnSet("name"));


                var user = new Entity("team");
                user.Attributes["name"] = "Test Team";
                var userid = orgAdminUIService.Create(user);

                var retrievedTeam = orgAdminUIService.Retrieve("team", userid, new ColumnSet(true));
                Assert.AreEqual(user.Attributes["name"], retrievedTeam.Attributes["name"]);
                var businessunitid = retrievedTeam.GetAttributeValue<EntityReference>("businessunitid").Id;
                var businessunit = orgAdminUIService.Retrieve("businessunit", businessunitid, new ColumnSet("name"));

                Assert.AreEqual(adminBusinessunit.Attributes["name"], businessunit.Attributes["name"]);
            }
        }

        [TestMethod]
        public void TestPopulateWith() {
            using (var context = new Xrm(orgAdminUIService)) {
                var id = Guid.NewGuid();
                var acc = new Account(id);
                acc.Name = "Dauda";
                crm.PopulateWith(acc);
                crm.ContainsEntity(acc);
            }
        }


        [TestMethod]
        public void TestCreateSameId() {
            using (var context = new Xrm(orgAdminUIService)) {
                var id = orgAdminUIService.Create(new Account());
                try {
                    orgAdminUIService.Create(new Account(id));
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

            }
        }

        [TestMethod]
        public void TestCreateWithRelatedEntities() {
            using (var context = new Xrm(orgAdminUIService)) {
                var contact = new Contact();
                var account1 = new Account();
                var account2 = new Account();
                account1.Name = "AccountRelated 1";
                account2.Name = "AccountRelated 2";

                var accounts = new EntityCollection(new List<Entity>() { account1, account2 });

                // Add related order items so it can be created in one request
                contact.RelatedEntities.Add(new Relationship {
                    PrimaryEntityRole = EntityRole.Referenced,
                    SchemaName = "somerandomrelation"
                }, accounts);

                var request = new CreateRequest {
                    Target = contact
                };
                try {
                    orgAdminUIService.Execute(request);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                contact = new Contact();
                contact.RelatedEntities.Add(new Relationship {
                    PrimaryEntityRole = EntityRole.Referenced,
                    SchemaName = "account_primary_contact"
                }, accounts);

                request = new CreateRequest {
                    Target = contact
                };

                contact.Id = (orgAdminUIService.Execute(request) as CreateResponse).id;
                var accountSet = context.AccountSet.Where(x => x.Name.StartsWith("AccountRelated")).ToList();
                Assert.AreEqual(2, accountSet.Count);
                foreach (var acc in accountSet) {
                    Assert.AreEqual(contact.Id, acc.PrimaryContactId.Id);
                }
            }
        }
    }
}

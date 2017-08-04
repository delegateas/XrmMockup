using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestRetrieve : UnitTestBase {

        [TestMethod]
        public void TestReferenceHasPrimaryAttribute() {
            using (var context = new Xrm(orgAdminUIService)) {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest" });
                var id2 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest2" });

                var acc1a = new Account(id1) {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id2)
                };
                this.orgAdminUIService.Update(acc1a);

                var retrieved = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                    new ColumnSet("accountid", "parentaccountid")).ToEntity<Account>();
                Assert.IsNotNull(retrieved.ParentAccountId);
                Assert.AreEqual(retrieved.Id, id1);

            }
        }

        [TestMethod]
        public void TestRetrieveHasId() {
            using (var context = new Xrm(orgAdminUIService)) {
                var accountName = "Litware, Inc.";
                var _accountId = orgAdminUIService.Create(
                new Account {
                    Name = accountName,
                    Address1_StateOrProvince = "Colorado"
                });

                var entity = (Account) orgAdminUIService.Retrieve(Account.EntityLogicalName, _accountId, new ColumnSet(true));
                Assert.AreEqual(_accountId, entity.Id);
                entity = (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName, _accountId, new ColumnSet("name"));
                Assert.AreEqual(_accountId, entity.Id);

                var calId = orgAdminUIService.Create(new Entity("calendar"));
                var cal = orgAdminUIService.Retrieve("calendar", calId, new ColumnSet(true));
                Assert.AreEqual(calId, cal.Id);
                cal = orgAdminUIService.Retrieve("calendar", calId, new ColumnSet("createdby"));
                Assert.AreEqual(calId, cal.Id);

            }
        }

        [TestMethod]
        public void TestRetrieveWithNullColumnset() {
            using (var context = new Xrm(orgAdminUIService)) {
                var account = new Account();
                account.Id = orgAdminUIService.Create(account);

                try {
                    orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, null);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
              

            }
        }


        [TestMethod]
        public void TestRetrieveRelatedEntities() {
            using (var context = new Xrm(orgAdminUIService)) {
                var accountName = "Litware, Inc.";
                var _accountId = orgAdminUIService.Create(
                new Account {
                    Name = accountName,
                    Address1_StateOrProvince = "Colorado"
                });

                // Create the two contacts.
                var _contact1Id = orgAdminUIService.Create(
                    new Contact() {
                        FirstName = "Ben",
                        LastName = "Andrews",
                        EMailAddress1 = "sample@example.com",
                        Address1_City = "Redmond",
                        Address1_StateOrProvince = "WA",
                        Address1_Telephone1 = "(206)555-5555",
                        ParentCustomerId = new EntityReference {
                            Id = _accountId,
                            LogicalName = Account.EntityLogicalName
                        }
                    });


                var _contact2Id = orgAdminUIService.Create(
                    new Contact() {
                        FirstName = "Alan",
                        LastName = "Wilcox",
                        EMailAddress1 = "sample@example.com",
                        Address1_City = "Bellevue",
                        Address1_StateOrProvince = "WA",
                        Address1_Telephone1 = "(425)555-5555",
                        ParentCustomerId = new EntityReference {
                            Id = _accountId,
                            LogicalName = Account.EntityLogicalName
                        }
                    });

                var _contact3Id = orgAdminUIService.Create(
                   new Contact() {
                       FirstName = "Colin",
                       LastName = "Wilcox",
                       EMailAddress1 = "sample@example.com",
                       Address1_City = "Bellevue",
                       Address1_StateOrProvince = "WA",
                       Address1_Telephone1 = "(425)555-5555",
                       ParentCustomerId = new EntityReference {
                           Id = new Guid(),
                           LogicalName = Account.EntityLogicalName
                       }
                   });


                //create the query expression object
                QueryExpression query = new QueryExpression();

                //Query on reated entity records
                query.EntityName = "contact";

                //Retrieve the all attributes of the related record
                query.ColumnSet = new ColumnSet(true);

                //create the relationship object
                Relationship relationship = new Relationship();

                //add the condition where you can retrieve only the account related active contacts
                query.Criteria = new FilterExpression();
                query.Criteria.AddCondition(new ConditionExpression("address1_city", ConditionOperator.Equal, "Bellevue"));

                // name of relationship between account & contact
                relationship.SchemaName = "contact_customer_accounts";

                //create relationshipQueryCollection Object
                RelationshipQueryCollection relatedEntity = new RelationshipQueryCollection();

                //Add the your relation and query to the RelationshipQueryCollection
                relatedEntity.Add(relationship, query);

                //create the retrieve request object
                RetrieveRequest request = new RetrieveRequest();

                //add the relatedentities query
                request.RelatedEntitiesQuery = relatedEntity;

                //set column to  and the condition for the account
                request.ColumnSet = new ColumnSet("accountid");
                request.Target = new EntityReference { Id = _accountId, LogicalName = "account" };

                //execute the request
                RetrieveResponse response = (RetrieveResponse)orgAdminUIService.Execute(request);

                Assert.AreEqual(1, response.Entity.RelatedEntities.Count);
                var collection = response.Entity.RelatedEntities.Values.First();
                Assert.AreEqual(1, collection.Entities.Count);
                var entity = collection.Entities.First();
                Assert.IsTrue(entity.Attributes.ContainsKey("firstname"));
                Assert.AreEqual("Alan", entity.Attributes["firstname"]);

        }
        }
    }

}

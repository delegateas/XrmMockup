using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestRetrieve : UnitTestBase
    {
        [TestMethod]
        public void TestReferenceHasPrimaryAttribute()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest" });
                var id2 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest2" });

                var acc1a = new Account(id1)
                {
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
        public void TestRetrieveHasId()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var accountName = "Litware, Inc.";
                var _accountId = orgAdminUIService.Create(
                new Account
                {
                    Name = accountName,
                    Address1_StateOrProvince = "Colorado"
                });

                var entity = (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName, _accountId, new ColumnSet(true));
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
        public void TestRetrieveWithNullColumnset()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminUIService.Create(account);

                try
                {
                    orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, null);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }


            }
        }


        [TestMethod]
        public void TestRetrieveRelatedEntities()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var accountName = "Litware, Inc.";
                var _accountId = orgAdminUIService.Create(
                new Account
                {
                    Name = accountName,
                    Address1_StateOrProvince = "Colorado"
                });

                // Create the two contacts.
                orgAdminUIService.Create(
                    new Contact()
                    {
                        FirstName = "Ben",
                        LastName = "Andrews",
                        EMailAddress1 = "sample@example.com",
                        Address1_City = "Redmond",
                        Address1_StateOrProvince = "WA",
                        Address1_Telephone1 = "(206)555-5555",
                        ParentCustomerId = new EntityReference
                        {
                            Id = _accountId,
                            LogicalName = Account.EntityLogicalName
                        }
                    });


                orgAdminUIService.Create(
                    new Contact()
                    {
                        FirstName = "Alan",
                        LastName = "Wilcox",
                        EMailAddress1 = "sample@example.com",
                        Address1_City = "Bellevue",
                        Address1_StateOrProvince = "WA",
                        Address1_Telephone1 = "(425)555-5555",
                        ParentCustomerId = new EntityReference
                        {
                            Id = _accountId,
                            LogicalName = Account.EntityLogicalName
                        }
                    });


                //create the query expression object
                QueryExpression query = new QueryExpression
                {
                    EntityName = "contact",
                    ColumnSet = new ColumnSet(true)
                };

                //create the relationship object
                Relationship relationship = new Relationship();

                //add the condition where you can retrieve only the account related active contacts
                query.Criteria = new FilterExpression();
                query.Criteria.AddCondition(new ConditionExpression("address1_city", ConditionOperator.Equal, "Bellevue"));

                // name of relationship between account & contact
                relationship.SchemaName = "contact_customer_accounts";

                //create relationshipQueryCollection Object
                RelationshipQueryCollection relatedEntity = new RelationshipQueryCollection
                {
                    { relationship, query }
                };

                //create the retrieve request object
                RetrieveRequest request = new RetrieveRequest
                {
                    RelatedEntitiesQuery = relatedEntity,
                    ColumnSet = new ColumnSet("accountid"),
                    Target = new EntityReference { Id = _accountId, LogicalName = "account" }
                };

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

        [TestMethod]
        public void TestFetchMoneyAttribute()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var invoice = new Invoice()
                {
                    Name = "test",
                    TotalAmount = 10m,
                };
                invoice.Id = orgGodService.Create(invoice);

                var retrievedSucceeds = orgAdminService.Retrieve(Invoice.EntityLogicalName, invoice.Id, new ColumnSet("totalamount", "transactioncurrencyid")) as Invoice;
                Assert.IsNotNull(retrievedSucceeds.TotalAmount);
                Assert.AreEqual(10m, retrievedSucceeds.TotalAmount.Value);

                var retrievedFails = orgAdminService.Retrieve(Invoice.EntityLogicalName, invoice.Id, new ColumnSet("totalamount")) as Invoice;
                Assert.IsNotNull(retrievedSucceeds.TotalAmount);
                Assert.AreEqual(10m, retrievedSucceeds.TotalAmount.Value);
            }
        }

        [TestMethod]
        public void TestFormattedValuesRetrieveMultiple()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var invoice = new Invoice()
                {
                    Name = "test",
                    PriorityCode = Invoice_PriorityCode.DefaultValue,
                };
                invoice.Id = orgAdminUIService.Create(invoice);

                var retrieved = context.InvoiceSet.FirstOrDefault();
                Assert.AreEqual("Default Value", retrieved.FormattedValues["prioritycode"]);
            }
        }

        [TestMethod]
        public void TestFormattedValuesRetrieve()
        {
            var invoice = new Invoice()
            {
                Name = "test",
                PriorityCode = Invoice_PriorityCode.DefaultValue,
            };
            invoice.Id = orgAdminUIService.Create(invoice);

            var retrieved = orgAdminUIService.Retrieve(Invoice.EntityLogicalName, invoice.Id, new ColumnSet(true));
            Assert.AreEqual("Default Value", retrieved.FormattedValues["prioritycode"]);
        }

        [TestMethod]
        public void TestRetrieveInvalidAttributeFails()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "SomeName" });
                var attr = "invalidatttributeaaaaaaaaaaaa";
                try
                {
                    orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                        new ColumnSet(attr)).ToEntity<Account>();
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(MockupException));
                    Assert.AreEqual($"'account' entity doesn't contain attribute with Name = '{attr}'", e.Message);
                }

            }
        }

        [TestMethod]
        public void TestCaseSensitivity()
        {
            var guid = Guid.NewGuid().ToString();

            var c = new Contact();
            c.FirstName = $"{guid}MATT";
            orgAdminService.Create(c);

            var q = new QueryExpression("contact");
            q.Criteria.AddCondition("firstname", ConditionOperator.Equal, $"{guid}matt");
            q.ColumnSet = new ColumnSet(true);
            var res = orgAdminService.RetrieveMultiple(q);


            Assert.AreEqual($"{guid}MATT", res.Entities.Single().GetAttributeValue<string>("firstname"));

        }

    }

}

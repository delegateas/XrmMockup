using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestRetrieve : UnitTestBase
    {
        public TestRetrieve(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
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
                Assert.NotNull(retrieved.ParentAccountId);
                Assert.Equal(retrieved.Id, id1);

            }
        }

        [Fact]
        public void TestLookupFormattedValues()
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
                Assert.NotNull(retrieved.ParentAccountId);
                Assert.Equal("MLJ UnitTest2", retrieved.FormattedValues["parentaccountid"]);
            }
        }

        [Fact]
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
                Assert.Equal(_accountId, entity.Id);
                entity = (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName, _accountId, new ColumnSet("name"));
                Assert.Equal(_accountId, entity.Id);

                var calId = orgAdminUIService.Create(new Entity("calendar"));
                var cal = orgAdminUIService.Retrieve("calendar", calId, new ColumnSet(true));
                Assert.Equal(calId, cal.Id);
                cal = orgAdminUIService.Retrieve("calendar", calId, new ColumnSet("createdby"));
                Assert.Equal(calId, cal.Id);

            }
        }

        [Fact]
        public void TestRetrieveWithNullColumnset()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminUIService.Create(account);

                try
                {
                    orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, null);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }


            }
        }


        [Fact]
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

                Assert.Single(response.Entity.RelatedEntities);
                var collection = response.Entity.RelatedEntities.Values.First();
                Assert.Single(collection.Entities);
                var entity = collection.Entities.First();
                Assert.True(entity.Attributes.ContainsKey("firstname"));
                Assert.Equal("Alan", entity.Attributes["firstname"]);

            }
        }

        [Fact]
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
                Assert.NotNull(retrievedSucceeds.TotalAmount);
                Assert.Equal(10m, retrievedSucceeds.TotalAmount.Value);

                var retrievedFails = orgAdminService.Retrieve(Invoice.EntityLogicalName, invoice.Id, new ColumnSet("totalamount")) as Invoice;
                Assert.NotNull(retrievedSucceeds.TotalAmount);
                Assert.Equal(10m, retrievedSucceeds.TotalAmount.Value);
            }
        }

        [Fact]
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
                Assert.Equal("Default Value", retrieved.FormattedValues["prioritycode"]);
            }
        }

        [Fact]
        public void TestFormattedValuesRetrieve()
        {
            var invoice = new Invoice()
            {
                Name = "test",
                PriorityCode = Invoice_PriorityCode.DefaultValue,
            };
            invoice.Id = orgAdminUIService.Create(invoice);

            var retrieved = orgAdminUIService.Retrieve(Invoice.EntityLogicalName, invoice.Id, new ColumnSet(true));
            Assert.Equal("Default Value", retrieved.FormattedValues["prioritycode"]);
        }

        [Fact]
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
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<MockupException>(e);
                    Assert.Equal($"'account' entity doesn't contain attribute with Name = '{attr}'", e.Message);
                }

            }
        }


#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015 || XRM_MOCKUP_TEST_2016)
        [Fact]
        public void TestEmptyCalculatedFieldss()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new dg_animal());
                dg_animal.Retrieve(orgAdminService, id1, x => x.dg_EmptyCalculatedField);
            }
        }
#endif
    }
}

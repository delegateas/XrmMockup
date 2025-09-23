using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestAssocDissoc : UnitTestBase
    {
        Account account1;
        Account account2;
        Account account3;

        Contact contact1;
        Contact contact2;

        public TestAssocDissoc(XrmMockupFixture fixture) : base(fixture)
        {
            contact1 = new Contact { FirstName = "Hans" };
            contact2 = new Contact { FirstName = "John" };
            account1 = new Account { Name = "Account 1" };
            account2 = new Account { Name = "Account 2" };
            account3 = new Account { Name = "Account 3" };

            contact1.Id = orgAdminUIService.Create(contact1);
            contact2.Id = orgAdminUIService.Create(contact2);
            account1.Id = orgAdminUIService.Create(account1);
            account2.Id = orgAdminUIService.Create(account2);
            account3.Id = orgAdminUIService.Create(account3);
        }

        [Fact]
        public void TestAssocDissoc1N()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id),
                    new EntityReference(Account.EntityLogicalName, account3.Id)
                };

                Relationship relationship = new Relationship("account_primary_contact");

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship,
                    relatedEntities);

                foreach (var acc in context.AccountSet.Where(x => x.Name.StartsWith("Account")))
                {
                    Assert.Equal(contact1.Id, acc.PrimaryContactId.Id);
                    Assert.Equal(Contact.EntityLogicalName, acc.PrimaryContactId.LogicalName);
                }

                context.ClearChanges();

                orgAdminUIService.Disassociate(Contact.EntityLogicalName, contact1.Id, relationship,
                    relatedEntities);

                foreach (var acc in context.AccountSet.Where(x => x.Name.StartsWith("Account")))
                {
                    Assert.Null(acc.PrimaryContactId);
                }
            }
        }

        [Fact]
        public void TestAssocDissocN1()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Contact.EntityLogicalName, contact1.Id)
                };

                Relationship relationship = new Relationship("account_primary_contact");

                orgAdminUIService.Associate(Account.EntityLogicalName, account1.Id, relationship,
                    relatedEntities);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)) as Account;

                Assert.Equal(contact1.Id, retrieved.PrimaryContactId.Id);

                orgAdminUIService.Disassociate(Account.EntityLogicalName, account1.Id, relationship,
                    relatedEntities);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)) as Account;

                Assert.Null(retrieved.PrimaryContactId);

                relatedEntities.Add(new EntityReference(Contact.EntityLogicalName, contact2.Id));
                try
                {
                    orgAdminUIService.Associate(Account.EntityLogicalName, account1.Id, relationship, relatedEntities);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        public void TestAssocDissocNN()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var relatedAccounts = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id),
                    new EntityReference(Account.EntityLogicalName, account3.Id)
                };

                var relatedContacts = new EntityReferenceCollection
                {
                    new EntityReference(Contact.EntityLogicalName, contact1.Id),
                    new EntityReference(Contact.EntityLogicalName, contact2.Id)
                };

                Relationship relationship = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship, relatedAccounts);

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact2.Id, relationship, relatedAccounts);

                var relationQuery = new RelationshipQueryCollection();
                var query = new QueryExpression(Account.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet("name")
                };
                relationQuery.Add(relationship, query);
                var req = new RetrieveRequest
                {
                    ColumnSet = new ColumnSet("firstname"),
                    RelatedEntitiesQuery = relationQuery,
                    Target = new EntityReference(Contact.EntityLogicalName, contact1.Id)
                };
                var retrievedContact = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Contact;
                var related = retrievedContact.RelatedEntities[relationship].Entities;
                Assert.Equal(3, related.Count());
                Assert.Equal(account1.Id, related.FirstOrDefault(e => (e as Account).Name == account1.Name).Id);
                Assert.Equal(account2.Id, related.FirstOrDefault(e => (e as Account).Name == account2.Name).Id);
                Assert.Equal(account3.Id, related.FirstOrDefault(e => (e as Account).Name == account3.Name).Id);

                orgAdminUIService.Disassociate(Contact.EntityLogicalName, contact1.Id, relationship, relatedAccounts);

                relationQuery = new RelationshipQueryCollection();
                query = new QueryExpression(Account.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet("name")
                };
                relationQuery.Add(relationship, query);
                req = new RetrieveRequest
                {
                    ColumnSet = new ColumnSet("firstname"),
                    RelatedEntitiesQuery = relationQuery,
                    Target = new EntityReference(Contact.EntityLogicalName, contact1.Id)
                };
                retrievedContact = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Contact;
                Assert.Equal(0, retrievedContact.RelatedEntities.Count);

                relationQuery = new RelationshipQueryCollection();
                query = new QueryExpression(Account.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet("name")
                };
                relationQuery.Add(relationship, query);
                req = new RetrieveRequest
                {
                    ColumnSet = new ColumnSet("firstname"),
                    RelatedEntitiesQuery = relationQuery,
                    Target = new EntityReference(Contact.EntityLogicalName, contact2.Id)
                };
                retrievedContact = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Contact;
                related = retrievedContact.RelatedEntities[relationship].Entities;
                Assert.Equal(3, related.Count());
                Assert.Equal(account1.Id, related.FirstOrDefault(e => (e as Account).Name == account1.Name).Id);
                Assert.Equal(account2.Id, related.FirstOrDefault(e => (e as Account).Name == account2.Name).Id);
                Assert.Equal(account3.Id, related.FirstOrDefault(e => (e as Account).Name == account3.Name).Id);
            }
        }

        [Fact]
        public void TestAssocNNTwoWay()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var relatedAccounts = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id),
                    new EntityReference(Account.EntityLogicalName, account3.Id)
                };

                var relatedContacts = new EntityReferenceCollection
                {
                    new EntityReference(Contact.EntityLogicalName, contact1.Id),
                    new EntityReference(Contact.EntityLogicalName, contact2.Id)
                };

                Relationship relationship = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Account.EntityLogicalName, account1.Id, relationship, relatedContacts);
                var relationQuery = new RelationshipQueryCollection();
                var query = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet("firstname")
                };
                relationQuery.Add(relationship, query);
                var req = new RetrieveRequest
                {
                    ColumnSet = new ColumnSet("name"),
                    RelatedEntitiesQuery = relationQuery,
                    Target = new EntityReference(Account.EntityLogicalName, account1.Id)
                };
                var retrievedAccount = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Account;
                var related = retrievedAccount.RelatedEntities[relationship].Entities;
                Assert.Equal(2, related.Count());
                Assert.Equal(contact1.Id, related.FirstOrDefault(e => (e as Contact).FirstName == contact1.FirstName).Id);
                Assert.Equal(contact2.Id, related.FirstOrDefault(e => (e as Contact).FirstName == contact2.FirstName).Id);
            }
        }

        [Fact]
        public void When_execute_is_called_with_a_non_existing_target_exception_is_thrown()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id),
                    new EntityReference(Account.EntityLogicalName, account3.Id)
                };

                Relationship relationship = new Relationship("account_primary_contact");
                try
                {
                    orgAdminUIService.Associate(Contact.EntityLogicalName, Guid.NewGuid(), relationship,
                        relatedEntities);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        public void When_execute_is_called_with_a_non_existing_reference_exception_is_thrown()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, Guid.NewGuid()),
                    new EntityReference(Account.EntityLogicalName, account3.Id)
                };

                Relationship relationship = new Relationship("account_primary_contact");
                try
                {
                    orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship,
                        relatedEntities);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        /*[ExpectedException(typeof(FaultException),
        "An existing relation contains the same link. N:N relation cannot be made.")]*/
        public void TestAssocNNTwice()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var relatedAccounts = new EntityReferenceCollection {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id),
                    new EntityReference(Account.EntityLogicalName, account3.Id)
                };

                Relationship relationship1 = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship1, relatedAccounts); //contact 1 associated to all accounts

                Relationship relationship2 = new Relationship(dg_account_contact.EntityLogicalName);

                try
                {
                    orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship2, relatedAccounts);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        public void TestAssociateWOPrimaryNamePlugin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();

                dg_bus bus = new dg_bus { dg_Ticketprice = 1 };
                bus.Id = orgAdminService.Create(bus);

                dg_child child = new dg_child { dg_name = "Margrethe" };
                child.Id = orgAdminService.Create(child);

                relatedEntities.Add(new EntityReference(dg_child.EntityLogicalName, child.Id));
                Relationship relationship = new Relationship("dg_bus_parental");

                orgAdminUIService.Associate(dg_bus.EntityLogicalName, bus.Id, relationship,
                    relatedEntities);

                var retrievedBus = dg_bus.Retrieve(orgAdminService, bus.Id, x => x.dg_Ticketprice);
                Assert.Equal(25, retrievedBus.dg_Ticketprice);
            }
        }

        [Fact]
        public void TestDisassociateWOPrimaryNamePlugin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();

                dg_bus bus = new dg_bus { dg_Ticketprice = 1 };
                bus.Id = orgAdminService.Create(bus);

                dg_child child = new dg_child { dg_name = "Margrethe", dg_parentBusId = bus.ToEntityReference() };
                child.Id = orgAdminService.Create(child);

                relatedEntities.Add(new EntityReference(dg_child.EntityLogicalName, child.Id));
                Relationship relationship = new Relationship("dg_bus_parental");

                orgAdminUIService.Disassociate(dg_bus.EntityLogicalName, bus.Id, relationship,
                    relatedEntities);

                var retrievedBus = dg_bus.Retrieve(orgAdminService, bus.Id, x => x.dg_Ticketprice);
                Assert.Equal(26, retrievedBus.dg_Ticketprice);
            }
        }
    }
}

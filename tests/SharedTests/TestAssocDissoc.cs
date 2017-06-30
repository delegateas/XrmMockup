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
using System.ServiceModel;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestAssocDissoc : UnitTestBase {
        Account account1;
        Account account2;
        Account account3;

        Contact contact1;
        Contact contact2;
        [TestInitialize]
        public void TestInitialize() {
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
        [TestMethod]
        public void TestAssocDissoc1N() {
            using (var context = new Xrm(orgAdminUIService)) {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account2.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account3.Id));

                Relationship relationship = new Relationship("account_primary_contact");

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship,
                    relatedEntities);

                foreach (var acc in context.AccountSet.Where(x => x.Name.StartsWith("Account"))) {
                    Assert.AreEqual(contact1.Id, acc.PrimaryContactId.Id);
                    Assert.AreEqual(Contact.EntityLogicalName, acc.PrimaryContactId.LogicalName);
                }

                context.ClearChanges();

                orgAdminUIService.Disassociate(Contact.EntityLogicalName, contact1.Id, relationship,
                    relatedEntities);

                foreach (var acc in context.AccountSet.Where(x => x.Name.StartsWith("Account"))) {
                    Assert.IsNull(acc.PrimaryContactId);
                }
            }
        }

        [TestMethod]
        public void TestAssocDissocN1() {
            using (var context = new Xrm(orgAdminUIService)) {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                relatedEntities.Add(new EntityReference(Contact.EntityLogicalName, contact1.Id));

                Relationship relationship = new Relationship("account_primary_contact");

                orgAdminUIService.Associate(Account.EntityLogicalName, account1.Id, relationship,
                    relatedEntities);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)) as Account;

                Assert.AreEqual(contact1.Id, retrieved.PrimaryContactId.Id);


                orgAdminUIService.Disassociate(Account.EntityLogicalName, account1.Id, relationship,
                    relatedEntities);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)) as Account;

                Assert.IsNull(retrieved.PrimaryContactId);

                relatedEntities.Add(new EntityReference(Contact.EntityLogicalName, contact2.Id));
                try {
                    orgAdminUIService.Associate(Account.EntityLogicalName, account1.Id, relationship, relatedEntities);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

            }
        }

        [TestMethod]
        public void TestAssocDissocNN() {
            using (var context = new Xrm(orgAdminUIService)) {
                var relatedAccounts = new EntityReferenceCollection();
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, account2.Id));
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, account3.Id));

                var relatedContacts = new EntityReferenceCollection();
                relatedContacts.Add(new EntityReference(Contact.EntityLogicalName, contact1.Id));
                relatedContacts.Add(new EntityReference(Contact.EntityLogicalName, contact2.Id));

                Relationship relationship = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship, relatedAccounts);

                orgAdminUIService.Associate(Contact.EntityLogicalName, contact2.Id, relationship, relatedAccounts);

                var relationQuery = new RelationshipQueryCollection();
                var query = new QueryExpression(Account.EntityLogicalName);
                query.ColumnSet = new ColumnSet("name");
                relationQuery.Add(relationship, query);
                var req = new RetrieveRequest();
                req.ColumnSet = new ColumnSet("firstname");
                req.RelatedEntitiesQuery = relationQuery;
                req.Target = new EntityReference(Contact.EntityLogicalName, contact1.Id);
                var retrievedContact = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Contact;
                var related = retrievedContact.RelatedEntities[relationship].Entities;
                Assert.AreEqual(3, related.Count());
                Assert.AreEqual(account1.Id, related.FirstOrDefault(e => (e as Account).Name == account1.Name).Id);
                Assert.AreEqual(account2.Id, related.FirstOrDefault(e => (e as Account).Name == account2.Name).Id);
                Assert.AreEqual(account3.Id, related.FirstOrDefault(e => (e as Account).Name == account3.Name).Id);


                orgAdminUIService.Disassociate(Contact.EntityLogicalName, contact1.Id, relationship, relatedAccounts);

                relationQuery = new RelationshipQueryCollection();
                query = new QueryExpression(Account.EntityLogicalName);
                query.ColumnSet = new ColumnSet("name");
                relationQuery.Add(relationship, query);
                req = new RetrieveRequest();
                req.ColumnSet = new ColumnSet("firstname");
                req.RelatedEntitiesQuery = relationQuery;
                req.Target = new EntityReference(Contact.EntityLogicalName, contact1.Id);
                retrievedContact = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Contact;
                Assert.AreEqual(0, retrievedContact.RelatedEntities.Count());

                relationQuery = new RelationshipQueryCollection();
                query = new QueryExpression(Account.EntityLogicalName);
                query.ColumnSet = new ColumnSet("name");
                relationQuery.Add(relationship, query);
                req = new RetrieveRequest();
                req.ColumnSet = new ColumnSet("firstname");
                req.RelatedEntitiesQuery = relationQuery;
                req.Target = new EntityReference(Contact.EntityLogicalName, contact2.Id);
                retrievedContact = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Contact;
                related = retrievedContact.RelatedEntities[relationship].Entities;
                Assert.AreEqual(3, related.Count());
                Assert.AreEqual(account1.Id, related.FirstOrDefault(e => (e as Account).Name == account1.Name).Id);
                Assert.AreEqual(account2.Id, related.FirstOrDefault(e => (e as Account).Name == account2.Name).Id);
                Assert.AreEqual(account3.Id, related.FirstOrDefault(e => (e as Account).Name == account3.Name).Id);

            }
        }

        [TestMethod]
        public void TestAssocNNTwoWay() {
            using (var context = new Xrm(orgAdminUIService)) {
                var relatedAccounts = new EntityReferenceCollection();
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, account2.Id));
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, account3.Id));

                var relatedContacts = new EntityReferenceCollection();
                relatedContacts.Add(new EntityReference(Contact.EntityLogicalName, contact1.Id));
                relatedContacts.Add(new EntityReference(Contact.EntityLogicalName, contact2.Id));

                Relationship relationship = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Account.EntityLogicalName, account1.Id, relationship, relatedContacts);
                var relationQuery = new RelationshipQueryCollection();
                var query = new QueryExpression(Contact.EntityLogicalName);
                query.ColumnSet = new ColumnSet("firstname");
                relationQuery.Add(relationship, query);
                var req = new RetrieveRequest();
                req.ColumnSet = new ColumnSet("name");
                req.RelatedEntitiesQuery = relationQuery;
                req.Target = new EntityReference(Account.EntityLogicalName, account1.Id);
                var retrievedAccount = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as Account;
                var related = retrievedAccount.RelatedEntities[relationship].Entities;
                Assert.AreEqual(2, related.Count());
                Assert.AreEqual(contact1.Id, related.FirstOrDefault(e => (e as Contact).FirstName == contact1.FirstName).Id);
                Assert.AreEqual(contact2.Id, related.FirstOrDefault(e => (e as Contact).FirstName == contact2.FirstName).Id);

            }
        }

        [TestMethod]
        public void When_execute_is_called_with_a_non_existing_target_exception_is_thrown() {
            using (var context = new Xrm(orgAdminUIService)) {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account2.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account3.Id));

                Relationship relationship = new Relationship("account_primary_contact");
                try {
                    orgAdminUIService.Associate(Contact.EntityLogicalName, Guid.NewGuid(), relationship,
                        relatedEntities);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
            }
        }

        [TestMethod]
        public void When_execute_is_called_with_a_non_existing_reference_exception_is_thrown() {
            using (var context = new Xrm(orgAdminUIService)) {
                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, Guid.NewGuid()));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account3.Id));

                Relationship relationship = new Relationship("account_primary_contact");
                try {
                    orgAdminUIService.Associate(Contact.EntityLogicalName, contact1.Id, relationship,
                        relatedEntities);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
            }
        }
    }

}

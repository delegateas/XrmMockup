using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmContext;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestRetrieveMultiple : UnitTestBase {

        Account account1;
        Account account2;
        Account account3;
        Account account4;

        Contact contact1;
        Contact contact2;
        Contact contact3;
        Contact contact4;

        Lead lead1;
        Lead lead2;
        Lead lead3;
        Lead lead4;

        [TestInitialize]
        public void TestInitialize() {
            account1 = new Account();
            account2 = new Account();
            account3 = new Account();
            account4 = new Account();

            account1.Name = "account1";
            account2.Name = "account2";
            account3.Name = "account3";
            account4.Name = "account4";

            account1.Address1_City = "Virum";
            account2.Address1_City = "Virum";
            account3.Address1_City = "Lyngby";
            account4.Address1_City = "Lyngby";

            account1.DoNotEMail = true;

            account1.Id = orgAdminUIService.Create(account1);
            account2.Id = orgAdminUIService.Create(account2);
            account3.Id = orgAdminUIService.Create(account3);
            account4.Id = orgAdminUIService.Create(account4);

            contact1 = new Contact();
            contact2 = new Contact();
            contact3 = new Contact();
            contact4 = new Contact();

            contact1.LastName = "contact1";
            contact2.LastName = "contact2";
            contact3.LastName = "contact3";
            contact4.LastName = "contact4";

            contact1.Id = orgAdminUIService.Create(contact1);
            contact2.Id = orgAdminUIService.Create(contact2);
            contact3.Id = orgAdminUIService.Create(contact3);
            contact4.Id = orgAdminUIService.Create(contact4);

            var rand = new Random();
            lead1 = new Lead() {
                Subject = "Some contact lead " + rand.Next(0, 1000),
                ParentContactId = contact1.ToEntityReference()
            };
            lead2 = new Lead() {
                Subject = "Some contact lead " + rand.Next(0, 1000),
                ParentContactId = contact2.ToEntityReference()
            };
            lead3 = new Lead() {
                Subject = "Some new lead " + rand.Next(0, 1000)
            };
            lead4 = new Lead() {
                Subject = "Some new lead " + rand.Next(0, 1000)
            };

            lead1.Id = orgAdminUIService.Create(lead1);
            lead2.Id = orgAdminUIService.Create(lead2);
            lead3.Id = orgAdminUIService.Create(lead3);
            lead4.Id = orgAdminUIService.Create(lead4);


        }

        [TestMethod]
        public void TestInnerJoin() {
            using (var context = new Xrm(orgAdminUIService)) {
                var res = account1.Id;

                var query =
                    from acc in context.AccountSet
                    join lead in context.LeadSet
                    on acc.AccountId equals lead.ParentAccountId.Id
                    where acc.Id == res
                    select new { acc.Name, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.AreEqual(2, result.Count());

                foreach (var r in result) {
                    Assert.AreEqual(account1.Name, r.Name);
                    Assert.IsTrue(r.Subject.StartsWith("Some"));
                }
            }
        }

        [TestMethod]
        public void TestFilterOnJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from con in context.ContactSet
                    join lead in context.LeadSet
                    on con.ContactId equals lead.ParentContactId.Id
                    where lead.Subject.StartsWith("Some") && con.LastName.StartsWith("contact")
                    select new { con.LastName, lead.Subject };

                var result = query.ToArray();
                Assert.AreEqual(2, result.Count());
            }
        }

        [TestMethod]
        public void TestAllColumns()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from con in context.ContactSet
                    where con.ContactId == contact1.Id
                    select con;

                var result = query.First();
                Assert.AreEqual(contact1.LastName, result.LastName);
            }
        }

        [TestMethod]
        public void TestFilterOnOptionSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                contact1.SetState(orgAdminUIService, ContactState.Inactive);

                var query =
                    from con in context.ContactSet
                    where con.StateCode == ContactState.Inactive
                    select con;

                var result = query.First();
                Assert.AreEqual(contact1.LastName, result.LastName);
            }
        }

        [TestMethod]
        public void TestOrderingJoin() {
            using (var context = new Xrm(orgAdminUIService)) {
                var res = account1.Id;

                var query =
                    from acc in context.AccountSet
                    join lead in context.LeadSet
                    on acc.AccountId equals lead.ParentAccountId.Id
                    orderby acc.Name descending, acc.AccountId
                    where acc.Name.StartsWith("account")
                    select new { acc.Name, acc.AccountId, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.AreEqual(8, result.Count());

                var ordered = result.OrderByDescending(x => x.Name).ThenBy(x => x.AccountId);
                CollectionAssert.AreEqual(ordered.ToList(), result.ToList());
            }
        }

        [TestMethod]
        public void BusinessUnitChange() {
            using (var context = new Xrm(orgAdminUIService)) {
                var rootBu = context.BusinessUnitSet.FirstOrDefault();
                Assert.IsNotNull(rootBu.Id);

                var anotherBusinessUnit = new BusinessUnit();
                anotherBusinessUnit.Id = orgAdminUIService.Create(anotherBusinessUnit);

                var user = crm.CreateUser(orgAdminUIService, anotherBusinessUnit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;

                // Create and check account user/bu
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.AreEqual(acc.OwnerId.Id, crm.AdminUser.Id);

                // Update and check new account user/bu
                var upd = new Account(acc.Id) {
                    OwnerId = user.ToEntityReference()
                };
                orgAdminUIService.Update(upd);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.AreEqual(acc.OwnerId.Id, user.Id);
                Assert.AreEqual(acc.OwningBusinessUnit.Id, user.BusinessUnitId.Id);
            }
        }

        [TestMethod]
        public void ContextJoinTest() {
            using (var context = new Xrm(this.orgAdminUIService)) {
                var acc1 = new Account() {
                    Name = "MLJ UnitTest " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id1 = this.orgAdminUIService.Create(acc1);

                var acc2 = new Account() {
                    Name = "MLJ UnitTest2 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id2 = this.orgAdminUIService.Create(acc2);

                var contact = new Contact() {
                    FirstName = "Jesper",
                    LastName = "Test"
                };
                contact.ParentCustomerId = new EntityReference(Account.EntityLogicalName, id2);
                var cid = this.orgAdminUIService.Create(contact);

                var acc1a = new Account(id1);
                acc1a.ParentAccountId = new EntityReference(Account.EntityLogicalName, id2);
                this.orgAdminUIService.Update(acc1a);


                var retrieved = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                    new ColumnSet("accountid")).ToEntity<Account>();
                context.Attach(retrieved);
                context.Load(retrieved, x => x.ParentAccountId);

                var retrieved2 = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id2,
                    new ColumnSet("accountid")).ToEntity<Account>();
                context.Attach(retrieved2);
                context.LoadEnumeration(retrieved, x => x.Referencedaccount_parent_account);

                var testEnumeration1 = context.LoadEnumeration(retrieved, x => x.Referencedaccount_parent_account);
                var testEnumeration2 = context.LoadEnumeration(retrieved2, x => x.Referencedaccount_parent_account);
                var testRelated1 = context.Load(retrieved, x => x.Referencingaccount_parent_account);
                var testRelated2 = context.Load(retrieved2, x => x.Referencingaccount_parent_account);

                var accountsWithContacts =
                    context.AccountSet
                        .Join<Account, Contact, Guid, object>(context.ContactSet, acc => acc.Id, c => c.ParentCustomerId.Id, (acc, c) => new { acc, c })
                        .FirstOrDefault();

                Assert.IsTrue(testEnumeration1.Count() == 0);
                Assert.IsTrue(testEnumeration2.Count() > 0);
                Assert.IsNotNull(testRelated1);
                Assert.IsNull(testRelated2);
                Assert.IsTrue(testRelated1.RelatedEntities.Count() > 0);
                Assert.IsTrue(testRelated1.RelatedEntities.Values.First().Entities.Count() > 0);
                Assert.AreEqual(testEnumeration2.Count(), testRelated1.RelatedEntities.Values.First().Entities.Count());
                Assert.AreEqual(retrieved.ParentAccountId.Id, id2);
                Assert.AreEqual(retrieved2.Referencedaccount_parent_account?.FirstOrDefault()?.Id, id1);
            }
        }


        [TestMethod]
        public void ContextLoadDifferentEntitiesTest() {
            using (var context = new Xrm(this.orgAdminUIService)) {
                var acc1 = new Account() {
                    Name = "MLJ UnitTest " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id1 = this.orgAdminUIService.Create(acc1);

                var acc2 = new Account() {
                    Name = "MLJ UnitTest2 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id2 = this.orgAdminUIService.Create(acc2);

                var contact = new Contact() {
                    FirstName = "Jesper",
                    LastName = "Test"
                };
                contact.ParentCustomerId = new EntityReference(Account.EntityLogicalName, id2);
                var cid = this.orgAdminUIService.Create(contact);

                var acc1a = new Account(id1);
                acc1a.ParentAccountId = new EntityReference(Account.EntityLogicalName, id2);
                this.orgAdminUIService.Update(acc1a);


                var retrieved = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id2,
                    new ColumnSet("accountid")).ToEntity<Account>();
                context.Attach(retrieved);

                var test = (IEnumerable<Contact>)context.LoadEnumeration<Account, object>(retrieved, x => x.contact_customer_accounts);

                Assert.IsTrue(test.Count() == 1);
                Assert.AreEqual(test.First().Id, cid);
            }
        }

        [TestMethod]
        public void TestLeftJoin() {
            using (var context = new Xrm(orgAdminUIService)) {

                var query =
                    from con in context.ContactSet
                    join lead in context.LeadSet
                    on con.ContactId equals lead.ParentContactId.Id into ls
                    from lead in ls.DefaultIfEmpty()
                    select new { con.ContactId, lead.Subject };

                Assert.AreEqual(4, query.AsEnumerable().Count());

                foreach (var r in query) {
                    Assert.IsTrue(r.Subject == null && (r.ContactId == contact3.Id || r.ContactId == contact4.Id) ||
                        r.Subject.StartsWith("Some contact lead") && (r.ContactId == contact1.Id || r.ContactId == contact2.Id));
                }
            }
        }

        [TestMethod]
        public void TestNestedJoins() {
            using (var context = new Xrm(orgAdminUIService)) {
                var res = contact1.Id;

                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account2.Id));


                orgAdminUIService.Associate(
                    Contact.EntityLogicalName, contact1.Id, new Relationship("account_primary_contact"), relatedEntities);

                var query =
                    from con in context.ContactSet
                    join acc in context.AccountSet
                    on con.ContactId equals acc.PrimaryContactId.Id
                    join lead in context.LeadSet
                    on acc.AccountId equals lead.ParentAccountId.Id
                    where con.Id == res
                    select new { con.LastName, acc.Name, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.AreEqual(4, result.Count());

                foreach (var r in result) {
                    Assert.AreEqual(contact1.LastName, r.LastName);
                    Assert.IsTrue(account1.Name == r.Name || account2.Name == r.Name);
                    Assert.IsTrue(r.Subject.StartsWith("Some"));
                }
            }
        }

        [TestMethod]
        public void TestNestedJoinsLinkedSelectOnId()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = contact1.Id;

                EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account1.Id));
                relatedEntities.Add(new EntityReference(Account.EntityLogicalName, account2.Id));


                orgAdminUIService.Associate(
                    Contact.EntityLogicalName, contact1.Id, new Relationship("account_primary_contact"), relatedEntities);

                var query =
                    from con in context.ContactSet
                    join acc in context.AccountSet
                    on con.ContactId equals acc.PrimaryContactId.Id
                    join lead in context.LeadSet
                    on acc.AccountId equals lead.ParentAccountId.Id
                    where con.Id == res
                    select new { con.Id, con.LastName, acc.Name, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.AreEqual(4, result.Count());

                foreach (var r in result)
                {
                    Assert.AreEqual(contact1.LastName, r.LastName);
                    Assert.IsTrue(account1.Name == r.Name || account2.Name == r.Name);
                    Assert.IsTrue(r.Subject.StartsWith("Some"));
                }
            }
        }

        [TestMethod]
        public void TestFilter() {
            using (var context = new Xrm(orgAdminUIService)) {

                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    select new { acc.Name };


                Assert.AreEqual(2, query.AsEnumerable().Count());

                foreach (var r in query) {
                    Assert.IsTrue(account1.Name == r.Name || account2.Name == r.Name);
                }
            }
        }

        [TestMethod]
        public void TestOrderByOtherAttributes()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    orderby acc.CreatedOn
                    select new { acc.AccountId };

                var result = query.ToArray();
                Assert.AreEqual(2, result.Length);
                Assert.AreEqual(account1.Id, result[0].AccountId);
                Assert.AreEqual(account2.Id, result[1].AccountId);
            }
        }

        [TestMethod]
        public void RetrieveMultipleNotEqualsNullCheck() {
            using (var context = new Xrm(orgAdminUIService)) {

                var acc = new Account() { };
                acc.Id = orgAdminService.Create(acc);

                var contact1 = new Contact() {
                    ParentCustomerId = acc.ToEntityReference()
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact() {
                    DoNotEMail = true,
                    ParentCustomerId = acc.ToEntityReference()
                };
                contact2.Id = orgAdminService.Create(contact2);


                var isTrue = context.ContactSet.Where(x => x.ParentCustomerId.Id == acc.Id && x.DoNotEMail == true).ToList();
                Assert.AreEqual(1, isTrue.Count);

                var isNotTrue = context.ContactSet.Where(x => x.ParentCustomerId.Id == acc.Id && x.DoNotEMail != true).ToList();
                Assert.AreEqual(1, isNotTrue.Count);
            }
        }

        [TestMethod]
        public void TestRetrieveMultipleFilterNullOr()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_Country == null || acc.Name == "account1"
                    select acc;

                var result = query.ToList();
                Assert.AreEqual(4, result.Count);
                var accResult = result.FirstOrDefault();
                Assert.AreEqual("account1", accResult.Name);
            }
        }

        [TestMethod]
        public void TestRetrieveMultipleFilterNullAnd()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_Country == null && acc.Name == "account1"
                    select acc;

                var result = query.ToList();
                Assert.AreEqual(1, result.Count);
                var accResult = result.FirstOrDefault();
                Assert.AreEqual("account1", accResult.Name);
            }
        }

        [TestMethod]
        public void TestRetrieveMultipleFilterGreaterThan5UnderlyingNull()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.AreEqual(0, result.Count);
            }
        }

        [TestMethod]
        public void TestRetrieveMultipleFilterNullOrGreaterThan()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees != null || acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.AreEqual(0, result.Count);
            }
        }

        [TestMethod]
        public void TestRetrieveMultipleFilterOrWithNullValue()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees == null && acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.AreEqual(0, result.Count);
            }
        }

        [TestMethod]
        public void TestContextWhere()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var userId = context.SystemUserSet.First().Id;
                //Currently returns null. Should return the same record.
                var reFetched = context.SystemUserSet.Single(a => a.Id == userId);
                Assert.IsNotNull(reFetched);
            }
        }
    }

}

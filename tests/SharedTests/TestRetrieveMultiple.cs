using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Globalization;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmContext;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestRetrieveMultiple : UnitTestBase
    {

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

        string guid = Guid.NewGuid().ToString();

        [TestInitialize]
        public void Init()
        {
            account1 = new Account();
            account2 = new Account();
            account3 = new Account();
            account4 = new Account();

            account1.Name = guid + "account1";
            account2.Name = guid + "account2";
            account3.Name = guid + "account3";
            account4.Name = guid + "account4";

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

            contact1.LastName = guid + "contact1";
            contact2.LastName = guid + "contact2";
            contact3.LastName = guid + "contact3";
            contact4.LastName = guid + "contact4";

            contact1.Id = orgAdminUIService.Create(contact1);
            contact2.Id = orgAdminUIService.Create(contact2);
            contact3.Id = orgAdminUIService.Create(contact3);
            contact4.Id = orgAdminUIService.Create(contact4);

            var rand = new Random();
            lead1 = new Lead()
            {
                Subject = guid + "Some contact lead " + rand.Next(0, 1000),
                ParentContactId = contact1.ToEntityReference()
            };
            lead2 = new Lead()
            {
                Subject = guid + "Some contact lead " + rand.Next(0, 1000),
                ParentContactId = contact2.ToEntityReference()
            };
            lead3 = new Lead()
            {
                Subject = guid + "Some new lead " + rand.Next(0, 1000)
            };
            lead4 = new Lead()
            {
                Subject = guid + "Some new lead " + rand.Next(0, 1000)
            };

            lead1.Id = orgAdminUIService.Create(lead1);
            lead2.Id = orgAdminUIService.Create(lead2);
            lead3.Id = orgAdminUIService.Create(lead3);
            lead4.Id = orgAdminUIService.Create(lead4);


        }

        [TestMethod]
        public void TestInnerJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = account1.Id;

                var query =
                    from acc in context.AccountSet
                    join lead in context.LeadSet
                    on acc.AccountId equals lead.ParentAccountId.Id
                    where acc.Id == res
                    select new { acc.Name, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.AreEqual(2, result.Count());

                foreach (var r in result)
                {
                    Assert.AreEqual(account1.Name, r.Name);
                    Assert.IsTrue(r.Subject.StartsWith("Some"));
                }
            }
        }

        // ignored until entityname can be handled correctly for 2011
#if !(XRM_MOCKUP_TEST_2011)
        [TestMethod]
        public void TestFilterOnJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from con in context.ContactSet
                    join lead in context.LeadSet
                    on con.ContactId equals lead.ParentContactId.Id
                    where lead.Subject.StartsWith(guid + "Some") && con.LastName.StartsWith(guid + "contact")
                    select new { con.LastName, lead.Subject };

                var result = query.ToArray();
                Assert.AreEqual(2, result.Count());
            }
        }
#endif
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
                    & con.LastName.StartsWith(guid)
                    select con;

                var result = query.First();
                Assert.AreEqual(contact1.LastName, result.LastName);
            }
        }

        [TestMethod]
        public void TestOrderingJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = account1.Id;

                var query =
                    from acc in context.AccountSet
                    join lead in context.LeadSet
                    on acc.AccountId equals lead.ParentAccountId.Id
                    orderby acc.Name descending, acc.AccountId
                    where acc.Name.StartsWith(guid+"account")
                    select new { acc.Name, acc.AccountId, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.AreEqual(8, result.Count());

                var ordered = result.OrderByDescending(x => x.Name).ThenBy(x => x.AccountId);
                CollectionAssert.AreEqual(ordered.ToList(), result.ToList());
            }
        }

        [TestMethod]
        public void BusinessUnitChange()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var rootBu = context.BusinessUnitSet.FirstOrDefault();

                var anotherBusinessUnit = new BusinessUnit();
                anotherBusinessUnit["name"] = "Business unit name";
                anotherBusinessUnit.Id = orgAdminUIService.Create(anotherBusinessUnit);

                var user = crm.CreateUser(orgAdminUIService, anotherBusinessUnit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;

                // Create and check account user/bu
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.AreEqual(acc.OwnerId.Id, crm.AdminUser.Id);

                // Update and check new account user/bu
                var upd = new Account(acc.Id)
                {
                    OwnerId = user.ToEntityReference()
                };
                orgAdminUIService.Update(upd);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.AreEqual(acc.OwnerId.Id, user.Id);
                Assert.AreEqual(acc.OwningBusinessUnit.Id, user.BusinessUnitId.Id);
            }
        }

        /// This commit in XrmContext makes it so the test fails. Consider the consequences of changing it back https://github.com/delegateas/XrmContext/commit/eb8a513517614e1e8cf4aca985eb465c39399acf#diff-d8e7c594f843646d7b0be2cccb990355
        [TestMethod, Ignore( "Error from commit in XrmContext")]
        public void ContextJoinTest()
        {
            using (var context = new Xrm(this.orgAdminUIService))
            {
                var acc1 = new Account()
                {
                    Name = "MLJ UnitTest " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id1 = orgAdminUIService.Create(acc1);

                var acc2 = new Account()
                {
                    Name = "MLJ UnitTest2 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id2 = orgAdminUIService.Create(acc2);

                var contact = new Contact()
                {
                    FirstName = "Jesper",
                    LastName = "Test"
                };
                contact.ParentCustomerId = new EntityReference(Account.EntityLogicalName, id2);
                var cid = orgAdminUIService.Create(contact);

                var acc1a = new Account(id1)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id2)
                };
                orgAdminUIService.Update(acc1a);


                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                    new ColumnSet("accountid")).ToEntity<Account>();
                context.Attach(retrieved);
                context.Load(retrieved, x => x.ParentAccountId);

                var retrieved2 = orgAdminUIService.Retrieve(Account.EntityLogicalName, id2,
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
        public void ContextLoadDifferentEntitiesTest()
        {
            using (var context = new Xrm(this.orgAdminUIService))
            {
                var acc1 = new Account()
                {
                    Name = "MLJ UnitTest " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id1 = orgAdminUIService.Create(acc1);

                var acc2 = new Account()
                {
                    Name = "MLJ UnitTest2 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
                var id2 = orgAdminUIService.Create(acc2);

                var contact = new Contact()
                {
                    FirstName = "Jesper",
                    LastName = "Test"
                };
                contact.ParentCustomerId = new EntityReference(Account.EntityLogicalName, id2);
                var cid = orgAdminUIService.Create(contact);

                var acc1a = new Account(id1)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id2)
                };
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
        public void TestLeftJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var query =
                    from con in context.ContactSet
                    join lead in context.LeadSet
                    on con.ContactId equals lead.ParentContactId.Id into ls
                    from lead in ls.DefaultIfEmpty()
                    where con.LastName.StartsWith(guid)
                    select new { con.ContactId, lead.Subject };

                Assert.AreEqual(4, query.AsEnumerable().Count());

                foreach (var r in query)
                {
                    Assert.IsTrue(r.Subject == null && (r.ContactId == contact3.Id || r.ContactId == contact4.Id) ||
                        r.Subject.StartsWith(guid+"Some contact lead") && (r.ContactId == contact1.Id || r.ContactId == contact2.Id));
                }
            }
        }

        [TestMethod]
        public void TestNestedJoins()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = contact1.Id;

                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id)
                };


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

                foreach (var r in result)
                {
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

                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id)
                };


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
        public void TestFilter()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    select new { acc.Name };


                Assert.IsTrue(query.ToList().Count() > 0);

                
            }
        }

        [TestMethod]
        public void TestOrderByOtherAttributes()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var guid = Guid.NewGuid().ToString();

                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    orderby acc.CreatedOn descending
                    select new { acc.AccountId };

                var result = query.ToList().Count();
                var acc1 = new Account() { Id = Guid.NewGuid() };
                acc1.Address1_City = "Virum";

                context.AddObject(acc1);
                context.SaveChanges();

                Assert.AreEqual(result + 1, query.ToList().Count);
                Assert.AreEqual(acc1.Id, query.First().AccountId);
            }
        }

        [TestMethod]
        public void RetrieveMultipleNotEqualsNullCheck()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var acc = new Account() { };
                acc.Id = orgAdminService.Create(acc);

                var contact1 = new Contact()
                {
                    ParentCustomerId = acc.ToEntityReference()
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact()
                {
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

                var result = query.ToList().Count();
                var acc1 = new Account() { Id = Guid.NewGuid() };
                acc1.Name = "account1";

                context.AddObject(acc1);
                context.SaveChanges();

                Assert.AreEqual(result + 1, query.ToList().Count);
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

                var result = query.ToList().Count();
                var acc1 = new Account() { Id = Guid.NewGuid() };
                acc1.Name = "account1";

                context.AddObject(acc1);
                context.SaveChanges();

                Assert.AreEqual(result + 1, query.ToList().Count);

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

                var result = query.ToList().Count(); ;


                var acc1 = new Account() { Id = Guid.NewGuid() };
                acc1.NumberOfEmployees = 27;
                context.AddObject(acc1);
                context.SaveChanges();

                Assert.AreEqual(result + 1, query.ToList().Count);
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

                var result = query.ToList().Count();
                var acc1 = new Account() { Id = Guid.NewGuid() };
                acc1.NumberOfEmployees = 27;

                context.AddObject(acc1);
                context.SaveChanges();

                Assert.AreEqual(result + 1, query.ToList().Count);
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

        [TestMethod]
        public void TestQueryExpressionIn()
        {
            var query = new QueryExpression("lead")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("parentcontactid", ConditionOperator.In, new Guid[] { contact1.Id, contact2.Id }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<Lead>();
            Assert.AreEqual(2, res.Count());
            Assert.IsTrue(res.Any(x => x.Id == lead1.Id));
            Assert.IsTrue(res.Any(x => x.Id == lead2.Id));
            Assert.IsTrue(res.Any(x => x.Description == "*** TEST VALUE ***"));
            Assert.IsTrue(res.Any(x => x.Description == null));
        }

        [TestMethod]
        public void TestQueryExpressionInEmpty()
        {
            var query = new QueryExpression("lead")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("parentcontactid", ConditionOperator.In, new Guid[] { }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<Lead>();
            Assert.AreEqual(0, res.Count());
        }

        [TestMethod]
        public void TestQueryExpressionNotIn()
        {
            var query = new QueryExpression("lead")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("parentcontactid", ConditionOperator.NotIn, new Guid[] { contact1.Id, contact2.Id }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<Lead>();
            Assert.IsTrue(!res.Any(x => x.Id == lead1.Id));
            Assert.IsTrue(!res.Any(x => x.Id == lead2.Id));
        }

        [TestMethod]
        public void TestQueryExpressionNotInEmpty()
        {
            var leadCount = 0;
            using (var context = new Xrm(orgAdminUIService))
            {
                leadCount = context.LeadSet.Select(x => x.LeadId).ToList().Count();
            }

            var query = new QueryExpression("lead")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("parentcontactid", ConditionOperator.NotIn, new Guid[] { }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<Lead>();
            Assert.AreEqual(leadCount, res.Count());
        }

#if !(XRM_MOCKUP_TEST_2011)

        [TestMethod]
        public void TestQueryExpressionLinkEntity()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, guid + "contact%"));
            query.Criteria = filter;

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "lead",
                LinkToAttributeName = "parentcontactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("subject"),
                EntityAlias = "lead"
            };
            var linkFilter = new FilterExpression(LogicalOperator.And);
            linkFilter.AddCondition(new ConditionExpression("lead", "subject", ConditionOperator.Like, guid + "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.AreEqual(2, res.Count());
        }


        [TestMethod]
        public void TestQueryExpressionLinkEntityNoSetEntityNameAndAlias()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, guid + "contact%"));
            query.Criteria = filter;

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "lead",
                LinkToAttributeName = "parentcontactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("subject"),
            };
            var linkFilter = new FilterExpression(LogicalOperator.And);
            linkFilter.AddCondition(new ConditionExpression("subject", ConditionOperator.Like, guid + "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.AreEqual(2, res.Count());
        }

        [TestMethod]
        public void TestQueryExpressionLinkEntityNoSetEntityName()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, guid + "contact%"));
            query.Criteria = filter;

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "lead",
                LinkToAttributeName = "parentcontactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("subject"),
                EntityAlias = "lead"
            };
            var linkFilter = new FilterExpression(LogicalOperator.And);
            linkFilter.AddCondition(new ConditionExpression("subject", ConditionOperator.Like, guid + "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.AreEqual(2, res.Count());
        }

#endif

        [TestMethod]
        public void RetrieveMultipleWithQueryByAttribute()
        {
            var result = orgAdminUIService.RetrieveMultiple(new QueryByAttribute
            {
                EntityName = Account.EntityLogicalName,
                Attributes = { "name" },
                Values = { account3.Name }
            });
            Assert.IsTrue(result.Entities.Count >0);
        }

        [TestMethod]
        public void RetrieveMultipleWithAliasedNullAttribute()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var result = (from lead in context.LeadSet
                              join contact in context.ContactSet on lead.ParentContactId.Id equals contact.Id
                              select new { lead.Subject, contact.FirstName, contact.LastName, contact.FullName })
                          .ToList();
                Assert.IsTrue(result.Count >0);
            }
        }

        [TestMethod]
        public void TestLINQContains()
        {
            var guid = Guid.NewGuid().ToString();

            var account = new Account()
            {
                Name = guid,

            };
            account.Id = orgAdminService.Create(account);

            using (var xrm = new Xrm(orgAdminService))
            {
                var query =
                    from c in xrm.AccountSet
                    where c.Name.Contains(guid)
                    select new
                    {
                        c.Name
                    };
                var queryList = query.ToList();
                Assert.IsTrue(queryList.Count == 1);
            }

            using (var xrm = new Xrm(orgAdminService))
            {
                var query =
                    from c in xrm.AccountSet
                    where !c.Name.Contains(guid) 
                    select new
                    {
                        c.AccountId
                    };
                var queryNotA = query.ToList();
                var all = (from c in xrm.AccountSet where c.Name != null select c).ToList();
                Assert.AreEqual(all.Count()-1, queryNotA.Count);
            }
        }

        [TestMethod]
        public void RetrieveMultipleTotalRecordCount()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet(true),
                    PageInfo = new PagingInfo() { ReturnTotalRecordCount = true }
                };
                query.Criteria.AddCondition("name", ConditionOperator.BeginsWith, guid);

                var res = orgAdminService.RetrieveMultiple(query);

                Assert.IsTrue(query.PageInfo.ReturnTotalRecordCount);
                Assert.AreEqual(4, res.TotalRecordCount);
            }
        }

        [TestMethod]
        public void RetrieveMultipleTotalRecordCountDefault()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet(true)
                };

                var res = orgAdminService.RetrieveMultiple(query);

                Assert.IsFalse(query.PageInfo.ReturnTotalRecordCount);
                Assert.AreEqual(-1, res.TotalRecordCount);
            }
        }

    }
}

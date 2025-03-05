using DG.Tools.XrmMockup;
using DG.XrmContext;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace DG.XrmMockupTest
{
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

        public TestRetrieveMultiple(XrmMockupFixture fixture) : base(fixture)
        {
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

            account1.Address1_PostalCode = "MK111DW";
            account2.Address1_PostalCode = "OX449NG";
            account3.Address1_PostalCode = "S103AY";
            account4.Address1_PostalCode = "SY71SW";

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
            lead1 = new Lead()
            {
                Subject = "Some contact lead " + rand.Next(0, 1000),
                ParentContactId = contact1.ToEntityReference()
            };
            lead2 = new Lead()
            {
                Subject = "Some contact lead " + rand.Next(0, 1000),
                ParentContactId = contact2.ToEntityReference()
            };
            lead3 = new Lead()
            {
                Subject = "Some new lead " + rand.Next(0, 1000)
            };
            lead4 = new Lead()
            {
                Subject = "Some new lead " + rand.Next(0, 1000)
            };

            lead1.Id = orgAdminUIService.Create(lead1);
            lead2.Id = orgAdminUIService.Create(lead2);
            lead3.Id = orgAdminUIService.Create(lead3);
            lead4.Id = orgAdminUIService.Create(lead4);
        }

        [Fact]
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
                Assert.Equal(2, result.Count());

                foreach (var r in result)
                {
                    Assert.Equal(account1.Name, r.Name);
                    Assert.StartsWith("Some", r.Subject);
                }
            }
        }

        [Fact]
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
                Assert.Equal(2, result.Count());
            }
        }

        [Fact]
        public void TestAllColumns()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from con in context.ContactSet
                    where con.ContactId == contact1.Id
                    select con;

                var result = query.First();
                Assert.Equal(contact1.LastName, result.LastName);
            }
        }

        [Fact]
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
                Assert.Equal(contact1.LastName, result.LastName);
            }
        }

        [Fact]
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
                    where acc.Name.StartsWith("account")
                    select new { acc.Name, acc.AccountId, lead.Subject };

                var result = query.AsEnumerable().Where(x => x.Subject.StartsWith("Some"));
                Assert.Equal(8, result.Count());

                var ordered = result.OrderByDescending(x => x.Name).ThenBy(x => x.AccountId);
                Assert.Equal(ordered.Select(x => new { x.Name, x.AccountId }).ToList(), result.Select(x => new { x.Name, x.AccountId }).ToList());
            }
        }

        [Fact]
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
                Assert.Equal(acc.OwnerId.Id, crm.AdminUser.Id);

                // Update and check new account user/bu
                var upd = new Account(acc.Id)
                {
                    OwnerId = user.ToEntityReference()
                };
                orgAdminUIService.Update(upd);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.Equal(acc.OwnerId.Id, user.Id);
                Assert.Equal(acc.OwningBusinessUnit.Id, user.BusinessUnitId.Id);
            }
        }

        /// This commit in XrmContext makes it so the test fails. Consider the consequences of changing it back https://github.com/delegateas/XrmContext/commit/eb8a513517614e1e8cf4aca985eb465c39399acf#diff-d8e7c594f843646d7b0be2cccb990355
        [Fact(Skip = "Error from commit in XrmContext")]
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

                Assert.True(testEnumeration1.Count() == 0);
                Assert.True(testEnumeration2.Count() > 0);
                Assert.NotNull(testRelated1);
                Assert.Null(testRelated2);
                Assert.True(testRelated1.RelatedEntities.Count() > 0);
                Assert.True(testRelated1.RelatedEntities.Values.First().Entities.Count() > 0);
                Assert.Equal(testEnumeration2.Count(), testRelated1.RelatedEntities.Values.First().Entities.Count());
                Assert.Equal(retrieved.ParentAccountId.Id, id2);
                Assert.Equal(retrieved2.Referencedaccount_parent_account?.FirstOrDefault()?.Id, id1);
            }
        }


        [Fact]
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

                Assert.True(test.Count() == 1);
                Assert.Equal(test.First().Id, cid);
            }
        }

        [Fact]
        public void TestLeftJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var query =
                    from con in context.ContactSet
                    join lead in context.LeadSet
                    on con.ContactId equals lead.ParentContactId.Id into ls
                    from lead in ls.DefaultIfEmpty()
                    select new { con.ContactId, lead.Subject };

                Assert.Equal(4, query.AsEnumerable().Count());

                foreach (var r in query)
                {
                    Assert.True(r.Subject == null && (r.ContactId == contact3.Id || r.ContactId == contact4.Id) ||
                        r.Subject.StartsWith("Some contact lead") && (r.ContactId == contact1.Id || r.ContactId == contact2.Id));
                }
            }
        }

        [Fact]
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
                Assert.Equal(4, result.Count());

                foreach (var r in result)
                {
                    Assert.Equal(contact1.LastName, r.LastName);
                    Assert.True(account1.Name == r.Name || account2.Name == r.Name);
                    Assert.StartsWith("Some", r.Subject);
                }
            }
        }

        [Fact]
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
                Assert.Equal(4, result.Count());

                foreach (var r in result)
                {
                    Assert.Equal(contact1.LastName, r.LastName);
                    Assert.True(account1.Name == r.Name || account2.Name == r.Name);
                    Assert.StartsWith("Some", r.Subject);
                }
            }
        }

        [Fact]
        public void TestFilter()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    select new { acc.Name };


                Assert.Equal(2, query.AsEnumerable().Count());

                foreach (var r in query)
                {
                    Assert.True(account1.Name == r.Name || account2.Name == r.Name);
                }
            }
        }

        [Fact]
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
                Assert.Equal(2, result.Length);
                Assert.Equal(account1.Id, result[0].AccountId);
                Assert.Equal(account2.Id, result[1].AccountId);
            }
        }

        [Fact]
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

                Assert.Single(isTrue);

                var isNotTrue = context.ContactSet.Where(x => x.ParentCustomerId.Id == acc.Id && x.DoNotEMail != true).ToList();
                Assert.Single(isNotTrue);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterNullOr()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_Country == null || acc.Name == "account1"
                    select acc;

                var result = query.ToList();
                Assert.Equal(4, result.Count);
                var accResult = result.FirstOrDefault();
                Assert.Equal("account1", accResult.Name);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterNullAnd()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_Country == null && acc.Name == "account1"
                    select acc;

                var result = query.ToList();
                Assert.Single(result);
                var accResult = result.FirstOrDefault();
                Assert.Equal("account1", accResult.Name);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterGreaterThan5UnderlyingNull()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.Empty(result);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterNullOrGreaterThan()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees != null || acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.Empty(result);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterOrWithNullValue()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees == null && acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.Empty(result);
            }
        }

        [Fact]
        public void TestContextWhere()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var userId = context.SystemUserSet.First().Id;
                //Currently returns null. Should return the same record.
                var reFetched = context.SystemUserSet.Single(a => a.Id == userId);
                Assert.NotNull(reFetched);
            }
        }

        [Fact]
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
            Assert.Equal(2, res.Count());
            Assert.Contains(res, x => x.Id == lead1.Id);
            Assert.Contains(res, x => x.Id == lead2.Id);
            Assert.Contains(res, x => x.Description == "*** TEST VALUE ***");
            Assert.Contains(res, x => x.Description == null);
        }

        [Fact]
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
            Assert.Empty(res);
        }

        [Fact]
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
            Assert.True(!res.Any(x => x.Id == lead1.Id));
            Assert.True(!res.Any(x => x.Id == lead2.Id));
        }

        [Fact]
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
            Assert.Equal(leadCount, res.Count());
        }

        [Fact]
        public void TestQueryExpressionLinkEntity()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));
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
            linkFilter.AddCondition(new ConditionExpression("lead", "subject", ConditionOperator.Like, "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Equal(2, res.Count());
        }


        [Fact]
        public void TestQueryExpressionLinkEntityNoSetEntityNameAndAlias()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));
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
            linkFilter.AddCondition(new ConditionExpression("subject", ConditionOperator.Like, "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Equal(2, res.Count());
        }

        [Fact]
        public void TestQueryExpressionLinkEntityNoSetEntityName()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));
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
            linkFilter.AddCondition(new ConditionExpression("subject", ConditionOperator.Like, "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Equal(2, res.Count());
        }

        [Fact]
        public void RetrieveMultipleWithLinkEntitiesReturnDistinctResults()
        {
            var lead = new Lead()
            {
                Subject = "Lead",
                ParentContactId = contact1.ToEntityReference()
            };
            orgAdminService.Create(lead);

            var linkEntity = new LinkEntity
            {
                LinkToEntityName = "lead",
                LinkToAttributeName = "parentcontactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet(false),
                EntityAlias = "contact",
                JoinOperator = JoinOperator.LeftOuter,
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Equal, contact1.LastName));

            var query = new QueryExpression("contact")
            {
                Distinct = true,
                ColumnSet = new ColumnSet(),
                LinkEntities = { linkEntity }
            };

            var res = orgAdminService.RetrieveMultiple(query).Entities;

            var distinctIdCount = res.Select(x => x.Id).Distinct().Count();

            Assert.Equal(distinctIdCount, res.Count);
        }

        [Fact]
        public void RetrieveMultipleWithQueryByAttribute()
        {
            var result = orgAdminUIService.RetrieveMultiple(new QueryByAttribute
            {
                EntityName = Account.EntityLogicalName,
                Attributes = { "name" },
                Values = { account3.Name }
            });
            Assert.Single(result.Entities);
        }

        [Fact]
        public void RetrieveMultipleWithAliasedNullAttribute()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var result = (from lead in context.LeadSet
                              join contact in context.ContactSet on lead.ParentContactId.Id equals contact.Id
                              select new { lead.Subject, contact.FirstName, contact.LastName, contact.FullName })
                          .ToList();
                Assert.Equal(2, result.Count);
            }
        }

        [Fact]
        public void TestLINQContains()
        {
            var account = new Account()
            {
                Name = "NotIn",

            };
            account.Id = orgAdminService.Create(account);

            var searchstring = "a";
            using (var xrm = new Xrm(orgAdminService))
            {
                var query =
                    from c in xrm.AccountSet
                    where c.Name.Contains(searchstring)
                    select new
                    {
                        c.Name
                    };
                var queryList = query.ToList();
                Assert.True(queryList.Count > 0);
            }

            using (var xrm = new Xrm(orgAdminService))
            {
                var query =
                    from c in xrm.AccountSet
                    where !c.Name.Contains(searchstring)
                    select new
                    {
                        c.AccountId
                    };
                var queryNotA = query.FirstOrDefault();
                Assert.Equal(account.Id, queryNotA.AccountId);
            }
        }

        [Fact]
        public void RetrieveMultipleTotalRecordCount()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet(true),
                    PageInfo = new PagingInfo() { ReturnTotalRecordCount = true }
                };

                var res = orgAdminService.RetrieveMultiple(query);

                Assert.True(query.PageInfo.ReturnTotalRecordCount);
                Assert.Equal(4, res.TotalRecordCount);
            }
        }

        [Fact]
        public void RetrieveMultipleTotalRecordCountDefault()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet(true)
                };

                var res = orgAdminService.RetrieveMultiple(query);

                Assert.False(query.PageInfo.ReturnTotalRecordCount);
                Assert.Equal(-1, res.TotalRecordCount);
            }
        }

        [Fact]
        public void RetrieveMultipleBeginsWith()
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.BeginsWith, "MK11");
            var res = orgAdminService.RetrieveMultiple(query);
            Assert.Single(res.Entities);

            query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.DoesNotBeginWith, "MK11");
            res = orgAdminService.RetrieveMultiple(query);
            Assert.Equal(3, res.Entities.Count);

            query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.EndsWith, "1DW");
            res = orgAdminService.RetrieveMultiple(query);
            Assert.Single(res.Entities);

            query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.DoesNotEndWith, "1DW");
            res = orgAdminService.RetrieveMultiple(query);
            Assert.Equal(3, res.Entities.Count);
        }
        [Fact]
        public void TestCaseSensitivity()
        {
            var c = new Contact();
            c.FirstName = "MATT";
            orgAdminService.Create(c);
            var q = new QueryExpression("contact");
            q.Criteria.AddCondition("firstname", ConditionOperator.Equal, "matt");
            q.ColumnSet = new ColumnSet(true);
            var res = orgAdminService.RetrieveMultiple(q);

            Assert.Equal("MATT", res.Entities.Single().GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void TestRetrieveMultipleFailWithNonExistentAttribute()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("x")
            };

            Assert.Throws<AggregateException>(() => orgAdminService.RetrieveMultiple(query));
        }

        [Fact]
        public void TestRetrieveMultipleTopCount()
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("accountid"),
                TopCount = 1
            };

            var res = orgAdminService.RetrieveMultiple(query);

            Assert.Single(res.Entities);
        }

        [Fact]
        public void TestRetrieveMultipleTake()
        {
            using (var context = new Xrm(orgAdminService))
            {
                Assert.Single(context.AccountSet
                    .Select(a => a.AccountId)
                    .Take(1)
                    .ToList());
            }
        }

        [Fact]
        public void TestRetrieveMultipleSkip()
        {
            using (var context = new Xrm(orgAdminService))
            {
                Assert.Empty(context.AccountSet
                    .Select(a => a.AccountId)
                    .Skip(4)
                    .ToList());

                Assert.Single(context.AccountSet
                    .Select(a => a.AccountId)
                    .Skip(3)
                    .ToList());
            }
        }

        [Fact]
        public void TestColumnComparison()
        {
            var sameName = new Contact()
            {
                FirstName = "Some strange name",
                LastName = "Some strange name",
            };
            sameName.Id = orgAdminService.Create(sameName);

            var retrieved = Contact.Retrieve(orgAdminService, sameName.Id, x => x.FirstName, x => x.LastName);
            Assert.Equal(sameName.FirstName, retrieved.FirstName);

            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression(Contact.EntityLogicalName)
                {
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(
                                Contact.EntityLogicalName,
                                Contact.GetColumnName<Contact>(x => x.FirstName),
                                ConditionOperator.Equal,
                                true,
                                Contact.GetColumnName<Contact>(x => x.LastName)),
                        }
                    }
                };

                var res = orgAdminService.RetrieveMultiple(query);
                var entities = res.Entities.Select(x => x.ToEntity<Contact>()).ToList();
                Assert.Single(entities);
                Assert.Equal(sameName.Id, entities[0].Id);
            }
        }
    }
}

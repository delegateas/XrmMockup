using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;
using Xunit.Sdk;
using System.Threading;

namespace DG.XrmMockupTest
{
    public class TestCreate : UnitTestBase
    {
        public TestCreate(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateOverriddenCreatedOn()
        {
            var dateTime = DateTime.UtcNow.AddHours(-12);
            var contact = new Contact()
            {
                OverriddenCreatedOn = dateTime,
            };
            contact.Id = orgAdminService.Create(contact);
            var dbContact = Contact.Retrieve(orgAdminService, contact.Id);
            Assert.True(dbContact.CreatedOn < dateTime.AddMinutes(1) && dbContact.CreatedOn > dateTime.AddMinutes(-1));
        }

        [Fact]
        public void TestCreateSimple()
        {
            var contact = new Contact()
            {
                FirstName = "John"
            };
            contact.Id = orgAdminService.Create(contact);

            var dbContact = Contact.Retrieve(orgAdminService, contact.Id);
            Assert.Equal(contact.FirstName, dbContact.FirstName);
        }

#if DATAVERSE_SERVICE_CLIENT
        [Fact]
        public async System.Threading.Tasks.Task TestCreateSimpleWithRetrieve()
        {
            var contact = new Contact()
            {
                FirstName = "John"
            };

            var created = await orgAdminService.CreateAndReturnAsync(contact, CancellationToken.None);

            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);

            var dbContact = Contact.Retrieve(orgAdminService, created.Id);
            Assert.Equal(dbContact.Attributes, created.Attributes);
        }
#endif

        [Fact]
        public void TestCreateWithRequest()
        {
            var contact = new Contact()
            {
                FirstName = "John"
            };
            var req = new CreateRequest()
            {
                Target = contact
            };
            var resp = orgAdminService.Execute(req) as CreateResponse;

            var dbContact = Contact.Retrieve(orgAdminService, resp.id);
            Assert.Equal(contact.FirstName, dbContact.FirstName);
        }

        [Fact]
        public void TestCreateWithNamedRequest()
        {
            var contact = new Contact()
            {
                FirstName = "John"
            };
            var req = new OrganizationRequest("Create");
            req.Parameters["Target"] = contact;

            var resp = orgAdminService.Execute(req) as CreateResponse;

            var dbContact = Contact.Retrieve(orgAdminService, resp.id);
            Assert.Equal(contact.FirstName, dbContact.FirstName);
        }


        [Fact]
        public void TestCreateWithUpdate()
        {
            var child = new dg_child()
            {
                dg_name = "Donald Duck"
            };

            child.Id = orgAdminUIService.Create(child);

            var resp = dg_child.Retrieve(orgAdminService, child.Id);
            Assert.Equal("Micky Mouse", resp.dg_name);
        }


        [Fact]
        public void TestUserCreation()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var adminUser = orgAdminUIService.Retrieve("systemuser", crm.AdminUser.Id, new ColumnSet("businessunitid"));
                var adminBusinessunitid = adminUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                var adminBusinessunit = orgAdminUIService.Retrieve("businessunit", adminBusinessunitid, new ColumnSet("name"));

                var user = new Entity("systemuser");
                user.Attributes["firstname"] = "Test User";
                var userid = orgAdminUIService.Create(user);

                var retrievedUser = orgAdminUIService.Retrieve("systemuser", userid, new ColumnSet(true));
                Assert.Equal(user.Attributes["firstname"], retrievedUser.Attributes["firstname"]);
                var businessunitid = retrievedUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                var businessunit = orgAdminUIService.Retrieve("businessunit", businessunitid, new ColumnSet("name"));

                Assert.Equal(adminBusinessunit.Attributes["name"], businessunit.Attributes["name"]);
            }
        }

        [Fact]
        public void TestTeamCreation()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var adminUser = orgAdminUIService.Retrieve("systemuser", crm.AdminUser.Id, new ColumnSet("businessunitid"));
                var adminBusinessunitid = adminUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                var adminBusinessunit = orgAdminUIService.Retrieve("businessunit", adminBusinessunitid, new ColumnSet("name"));

                var user = new Entity("team");
                user.Attributes["name"] = "Test Team";
                var userid = orgAdminUIService.Create(user);

                var retrievedTeam = orgAdminUIService.Retrieve("team", userid, new ColumnSet(true));
                Assert.Equal(user.Attributes["name"], retrievedTeam.Attributes["name"]);
                var businessunitid = retrievedTeam.GetAttributeValue<EntityReference>("businessunitid").Id;
                var businessunit = orgAdminUIService.Retrieve("businessunit", businessunitid, new ColumnSet("name"));

                Assert.Equal(adminBusinessunit.Attributes["name"], businessunit.Attributes["name"]);
            }
        }

        [Fact]
        public void TestPopulateWith()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id = Guid.NewGuid();
                var acc = new Account(id)
                {
                    Name = "Dauda"
                };
                crm.PopulateWith(acc);
                crm.ContainsEntity(acc);
            }
        }


        [Fact]
        public void TestCreateSameId()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id = orgAdminUIService.Create(new Account());
                try
                {
                    orgAdminUIService.Create(new Account(id));
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        public void TestCreateWithRelatedEntities()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var contact = new Contact();
                var account1 = new Account();
                var account2 = new Account();
                account1.Name = "AccountRelated 1";
                account2.Name = "AccountRelated 2";

                var accounts = new EntityCollection(new List<Entity>() { account1, account2 });

                // Add related order items so it can be created in one request
                contact.RelatedEntities.Add(new Relationship
                {
                    PrimaryEntityRole = EntityRole.Referenced,
                    SchemaName = "somerandomrelation"
                }, accounts);

                var request = new CreateRequest
                {
                    Target = contact
                };
                try
                {
                    orgAdminUIService.Execute(request);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                contact = new Contact();
                contact.RelatedEntities.Add(new Relationship
                {
                    PrimaryEntityRole = EntityRole.Referenced,
                    SchemaName = "account_primary_contact"
                }, accounts);

                request = new CreateRequest
                {
                    Target = contact
                };

                contact.Id = (orgAdminUIService.Execute(request) as CreateResponse).id;
                var accountSet = context.AccountSet.Where(x => x.Name.StartsWith("AccountRelated")).ToList();
                Assert.Equal(2, accountSet.Count);
                foreach (var acc in accountSet)
                {
                    Assert.Equal(contact.Id, acc.PrimaryContactId.Id);
                }
            }
        }

        [Fact]
        public void CreatingAttributeWithEmptyStringShouldReturnNull()
        {
            var id = orgAdminUIService.Create(new Lead { Subject = string.Empty });
            var lead = orgAdminService.Retrieve<Lead>(id);
            Assert.Null(lead.Subject);
        }

        [Fact]
        public void CreatingEntityWithSdkModeShouldInitializeBooleanAttributes()
        {
            var id = orgAdminService.Create(new Lead());
            var lead = orgAdminService.Retrieve<Lead>(id);
            Assert.NotNull(lead.DoNotBulkEMail);
            Assert.NotNull(lead.DoNotEMail);
            Assert.NotNull(lead.DoNotFax);
            Assert.NotNull(lead.DoNotPhone);
            Assert.NotNull(lead.DoNotPostalMail);
            Assert.NotNull(lead.DoNotSendMM);
        }

        [Fact]
        public void CreatingEntityWithSdkModeShouldInitializePicklistAttributes()
        {
            var id = orgAdminService.Create(new Lead());
            var lead = orgAdminService.Retrieve<Lead>(id);
            Assert.NotNull(lead.LeadQualityCode);
            Assert.NotNull(lead.PreferredContactMethodCode);
            Assert.NotNull(lead.PriorityCode);
            Assert.NotNull(lead.SalesStageCode);
        }


        [Fact]
        public void CreateEntityWithRelatedEntitiesShouldAssociateCorrectly()
        {
            // Arrange
            var testName = nameof(CreateEntityWithRelatedEntitiesShouldAssociateCorrectly);
            var account = new Account() { Name = testName };
            var relatedContacts = new[] { new Contact() { LastName = testName, EMailAddress1 = $"{testName}@delegate.delegate" } };
            account.contact_customer_accounts = relatedContacts;

            // Act (create & retrieve)
            var createdAccountId = orgAdminService.Create(account);
            var query = new QueryExpression(Account.EntityLogicalName) { ColumnSet = new ColumnSet(true), Criteria = new FilterExpression() };
            query.Criteria.AddCondition(new ConditionExpression(Account.GetColumnName<Account>(a => a.AccountId), ConditionOperator.Equal, createdAccountId));
            query.LinkEntities.Add(new LinkEntity
            {
                Columns = new ColumnSet(true),
                EntityAlias = Account.GetColumnName<Account>(a => a.contact_customer_accounts),
                JoinOperator = JoinOperator.LeftOuter,
                LinkFromEntityName = Account.EntityLogicalName,
                LinkToEntityName = Contact.EntityLogicalName,
                LinkFromAttributeName = Account.GetColumnName<Account>(a => a.AccountId),
                LinkToAttributeName = Contact.GetColumnName<Contact>(c => c.contact_customer_accounts)
            });
            var retrievedAccount = orgAdminService.RetrieveMultiple(query).Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedAccount);
            Assert.Contains(retrievedAccount.Attributes, attr => attr.Key.Contains(Account.GetColumnName<Account>(a => a.contact_customer_accounts)));
        }
    }
}

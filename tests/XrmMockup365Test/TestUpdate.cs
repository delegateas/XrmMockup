using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System.ServiceModel;

namespace DG.XrmMockupTest
{
    public class TestUpdate : UnitTestBase
    {
        public TestUpdate(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void UpdatingAttributeWithEmptyStringShouldReturnNull()
        {
            var id = orgAdminUIService.Create(new Lead { Subject = "nonemptystring" });
            orgAdminUIService.Update(new Lead { Id = id, Subject = string.Empty });
            var lead = orgAdminService.Retrieve<Lead>(id);
            Assert.Null(lead.Subject);
        }


        [Fact]
        public void UpdatingEntityWithRelatedEntitiesShouldAssociateCorrectly()
        {
            // Arrange
            var testName = nameof(UpdatingEntityWithRelatedEntitiesShouldAssociateCorrectly);
            var account = new Account() { Name = testName };
            var contact = new Contact() { LastName = testName, EMailAddress1 = $"{testName}@delegate.delegate" };

            // Act (create & retrieve)
            // create/arrange
            var createdAccountId = orgAdminService.Create(account);
            var createdContactId = orgAdminService.Create(contact); // SDK requires related entity exists before update (works on create)
            // update & retrieve
            account.Id = createdAccountId;
            account.contact_customer_accounts = new[] { new Contact(createdContactId) };
            orgAdminService.Update(account);
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
            var retrievedContact = Contact.Retrieve(orgAdminService, createdContactId);

            // Assert
            Assert.NotNull(retrievedAccount);
            Assert.Equal(createdAccountId, retrievedContact.ParentCustomerId.Id);
            Assert.Contains(retrievedAccount.Attributes, attr => attr.Key.Contains(Account.GetColumnName<Account>(a => a.contact_customer_accounts)));
        }

        [Fact]
        public void UpdatingEntityWithRelatedEntitiesNotCreatedThrowsError()
        {
            // Arrange
            var testName = nameof(UpdatingEntityWithRelatedEntitiesShouldAssociateCorrectly);
            var account = new Account() { Name = testName };
            var contact = new Contact() { LastName = testName, EMailAddress1 = $"{testName}@delegate.delegate" };

            // Act & Assert
            var createdAccountId = orgAdminService.Create(account);
            account.Id = createdAccountId;
            account.contact_customer_accounts = new[] { contact };
            Assert.Throws<FaultException>(() => orgAdminService.Update(account));
        }
    }
}

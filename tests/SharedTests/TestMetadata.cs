using System;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;
using System.Linq;
using Xunit.Sdk;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace DG.XrmMockupTest
{
    public class TestMetadata : UnitTestBase
    {
        public TestMetadata(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestRetrieveOptionSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionRetrieved = orgAdminUIService.Execute(new RetrieveOptionSetRequest() { Name = "workflow_stage" }) as RetrieveOptionSetResponse;
                Assert.True(optionRetrieved.OptionSetMetadata.Name == "workflow_stage");

            }
        }

        [Fact]
        public void TestRetrieveAllOptionSets()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionsRetrieved = orgAdminUIService.Execute(new RetrieveAllOptionSetsRequest()) as RetrieveAllOptionSetsResponse;
                Assert.Contains(optionsRetrieved.OptionSetMetadata, x => x.Name == "workflow_stage");

            }
        }

        [Fact]
        public void TestSetttingAttributes()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc.Attributes["name"] = "Jon";
                orgAdminUIService.Update(acc);

                try
                {
                    acc.Attributes["illegalName"] = 1;
                    orgAdminUIService.Update(acc);
                    throw new XunitException("FaultException should have been thrown");
                }
                catch (FaultException)
                {
                }
                catch (Exception e)
                {
                    throw new XunitException(
                         string.Format("Unexpected exception of type {0} caught: {1}",
                                        e.GetType(), e.Message)
                    );
                }
            }
        }

        [Fact]
        public void TestCRURestrictions()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                acc.Attributes.Add("opendeals_state", 22);
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("opendeals_state")) as Account;
                Assert.NotEqual(22, retrieved.OpenDeals_State);

                orgAdminUIService.Update(acc);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("opendeals_state")) as Account;
                Assert.NotEqual(22, retrieved.OpenDeals_State);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("isprivate")) as Account;
                Assert.False(retrieved.Attributes.ContainsKey("isprivate"));

            }
        }


        [Fact]
        public void RetrieveEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.NotNull(resp);
            Assert.NotNull(resp.EntityMetadata);
            Assert.Equal(req.LogicalName, resp.EntityMetadata.LogicalName);
        }

        [Fact]
        public void RetrieveAllFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.All
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.NotNull(resp.EntityMetadata.Privileges);
            Assert.NotNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.NotNull(resp.EntityMetadata.Attributes);
        }


        [Fact]
        public void RetrieveAttributesFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Attributes
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.NotNull(resp.EntityMetadata.Attributes);
        }


        [Fact]
        public void RetrievePrivilegesFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Privileges
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.NotNull(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }


        [Fact]
        public void RetrieveRelationshipsFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Relationships
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.NotNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }

        [Fact]
        public void RetrieveEntityFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Entity
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }

        [Fact]
        public void RetrieveDefaultFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Default
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }

        [Fact]
        public void RetrieveMetadataChangesRequest()
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode ", MetadataConditionOperator.Equals, 1));
            var propertyExpression = new MetadataPropertiesExpression { AllProperties = false };
            propertyExpression.PropertyNames.Add("LogicalName");
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter,
                Properties = propertyExpression
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Equal("account", response.EntityMetadata[0].LogicalName);

        }
    }

}

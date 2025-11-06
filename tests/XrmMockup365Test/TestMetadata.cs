using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using DG.XrmContext;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using Xunit;
using Xunit.Sdk;
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

        /*
        [Fact]
        public void TestRetrieveBoleanWithoutTrueOptionFalseOptionName()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var entity = new Entity_Ent();
                entity.Attributes.Add("name", "Name123");
                entity.Id = orgAdminUIService.Create(entity);
                var retrieved = orgAdminUIService.Retrieve(Entity_Ent.EntityLogicalName, entity.Id, new ColumnSet("name")) as Entity_Ent;
            }
        }
        */

        [Fact]
        public void TestRetrieveReferenceWithoutPrimaryNameAttribute()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var teamMembership = new TeamMembership();
                teamMembership.Attributes.Add("versionnumber", 1);
                teamMembership.Id = orgAdminUIService.Create(teamMembership);
                var testEntity = new TestEntity();
                testEntity.Attributes.Add("teammembership", new EntityReference("teammembership", teamMembership.Id));
                var fieldInfo = typeof(XrmMockupBase).GetField("Core", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? throw new FieldAccessException("Access to 'Core' is not possible.");
                var core = fieldInfo.GetValue(crm);
                fieldInfo = core.GetType().GetField("db", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? throw new FieldAccessException("Access to 'db' is not possible.");
                var xrmDb = fieldInfo.GetValue(core);
                Utility.PopulateEntityReferenceNames(testEntity, (XrmDb)xrmDb);
            }
        }
    }
}

namespace DG.XrmFramework.BusinessDomain.ServiceContext
{
    public enum TestEntityState
    {

        [EnumMember()]
        Active = 0,
    };

    public enum TestEntityStatusCode
    {

        [EnumMember()]
        Active = 1,
    };

    [EntityLogicalName("testentity")]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [DataContract()]
    public partial class TestEntity : ExtendedEntity<TestEntityState, TestEntityStatusCode>
    {
        public const string EntityLogicalName = "testentity";

        public const int EntityTypeCode = 10404;

        public TestEntity() :
                base(EntityLogicalName)
        {
        }

        public TestEntity(Guid Id) :
                base(EntityLogicalName, Id)
        {
        }

        private string DebuggerDisplay
        {
            get
            {
                return GetDebuggerDisplay("testentity");
            }
        }

        [AttributeLogicalName("teammembership")]
        [RelationshipSchemaName("lk_testentity_teammembership")]
        public TeamMembership NewEntity_TeamMembership
        {
            get
            {
                return GetRelatedEntity<TeamMembership>("lk_testentity_teammembership", null);
            }
            set
            {
                SetRelatedEntity("lk_testentity_teammembership", null, value);
            }
        }

        public static dg_animal Retrieve(IOrganizationService service, Guid id, params Expression<Func<dg_animal, object>>[] attrs)
        {
            return service.Retrieve(id, attrs);
        }
    }
}
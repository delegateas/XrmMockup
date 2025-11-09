using DG.XrmContext;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
namespace DG.XrmMockupTest
{
    public enum TestEntityState
    {
        [EnumMember()]
        Active = 0,
    }

    public enum TestEntityStatusCode
    {
        [EnumMember()]
        Active = 1,
    }

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
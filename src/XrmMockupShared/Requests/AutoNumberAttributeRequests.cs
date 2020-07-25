#if XRM_MOCKUP_365
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    #region SetAutoNumberSeedRequest

    internal class SetAutoNumberSeedRequestHandler : RequestHandler
    {
        internal SetAutoNumberSeedRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) :
            base(core, db, metadata, security, "SetAutoNumberSeed")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var helper = new AutoNumberAttributeHelperMethods();
            var request = MakeRequest<SetAutoNumberSeedRequest>(orgRequest);
            
            if (request.Value < 0)
            {
                throw new FaultException(
                    $"Cannot set Auto Number seed for attribute {request.AttributeName} of entity {request.EntityName} with value {request.Value} as it is less than 0.");
            }
            helper.CheckAutoAttributeMetadata(metadata, request.EntityName, request.AttributeName);

            var key = new Tuple<string, string>(request.EntityName, request.AttributeName);
            core.AutoNumberValues[key] = request.Value;
            core.AutoNumberSeeds[key] = request.Value;

            return new SetAutoNumberSeedResponse {ResponseName = RequestName};
        }
    }

    #endregion

    #region GetNextAutoNumberValueRequest

    internal class GetNextAutoNumberValueRequestHandler : RequestHandler
    {
        internal GetNextAutoNumberValueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            :
            base(core, db, metadata, security, "GetNextAutoNumberValue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var helper = new AutoNumberAttributeHelperMethods();
            var request = MakeRequest<GetNextAutoNumberValueRequest>(orgRequest);

            helper.CheckAutoAttributeMetadata(metadata, request.EntityName, request.AttributeName);

            var key = new Tuple<string, string>(request.EntityName, request.AttributeName);
            var nextValue = helper.DefaultSeed;
            if (core.AutoNumberValues.ContainsKey(key))
            {
                nextValue = core.AutoNumberValues[key];
            }

            return new GetNextAutoNumberValueResponse
            {
                ResponseName = RequestName,
                Results = new ParameterCollection
                {
                    {"NextAutoNumberValue", nextValue}
                }
            };
        }
    }

    #endregion

    #region GetAutoNumberSeedRequest

    internal class GetAutoNumberSeedRequestHandler : RequestHandler
    {
        internal GetAutoNumberSeedRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) :
            base(core, db, metadata, security, "GetAutoNumberSeed")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var helper = new AutoNumberAttributeHelperMethods();
            var request = MakeRequest<GetAutoNumberSeedRequest>(orgRequest);

            helper.CheckAutoAttributeMetadata(metadata, request.EntityName, request.AttributeName);

            var key = new Tuple<string, string>(request.EntityName, request.AttributeName);
            var seedValue = helper.DefaultSeed;
            if (core.AutoNumberSeeds.ContainsKey(key))
            {
                seedValue = core.AutoNumberSeeds[key];
            }

            return new GetAutoNumberSeedResponse
            {
                ResponseName = RequestName,
                Results = new ParameterCollection
                {
                    {"AutoNumberSeedValue", seedValue}
                }
            };
        }
    }

    #endregion

    #region AutoNumberAttributeHelperMethods

    internal class AutoNumberAttributeHelperMethods
    {
        public long DefaultSeed = 1000L;

        public void CheckAutoAttributeMetadata(MetadataSkeleton metadata, string entityName, string attributeName)
        {
            var metadataExist = metadata.EntityMetadata.TryGetValue(entityName, out var entityMetadata);
            var attrMetadata =
                entityMetadata?.Attributes.SingleOrDefault(attr => attr.LogicalName == attributeName);

            if (!metadataExist || attrMetadata == null || string.IsNullOrEmpty(attrMetadata.AutoNumberFormat))
            {
                throw new FaultException(
                    $"Attribute {attributeName} of entity {entityName} is not an Auto Number attribute. Please confirm the inputs " +
                    "for Attribute and Entity correctly map to an Auto Number attribute.");
            }
        }
    }

    #endregion
}
#endif
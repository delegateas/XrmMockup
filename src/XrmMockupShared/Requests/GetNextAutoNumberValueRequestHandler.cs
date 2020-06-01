#if XRM_MOCKUP_365
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class GetNextAutoNumberValueRequestHandler : RequestHandler
    {
        internal GetNextAutoNumberValueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            :
            base(core, db, metadata, security, "GetNextAutoNumberValue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<GetNextAutoNumberValueRequest>(orgRequest);

            var metadataExist = metadata.EntityMetadata.TryGetValue(request.EntityName, out var entityMetadata);
            var attrMetadata =
                entityMetadata?.Attributes.SingleOrDefault(attr => attr.LogicalName == request.AttributeName);

            if (!metadataExist || attrMetadata == null || string.IsNullOrEmpty(attrMetadata.AutoNumberFormat))
            {
                throw new FaultException(
                    $"Attribute {request.AttributeName} of entity {request.EntityName} is not an Auto Number attribute. Please confirm the inputs " +
                    "for Attribute and Entity correctly map to an Auto Number attribute.");
            }

            var key = request.EntityName + request.AttributeName;
            var nextValue = core.autoNumberValues[key] + 1;

            return new GetNextAutoNumberValueResponse
            {
                ResponseName = "SetAutoNumberSeed",
                Results = new ParameterCollection
                {
                    {"NextAutoNumberValue", nextValue}
                }
            };
        }
    }
}
#endif
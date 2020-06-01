#if XRM_MOCKUP_365
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class SetAutoNumberSeedRequestHandler : RequestHandler
    {
        internal SetAutoNumberSeedRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) :
            base(core, db, metadata, security, "SetAutoNumberSeed")
        { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SetAutoNumberSeedRequest>(orgRequest);

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
            core.autoNumberValues[key] = request.Value;

            return new SetAutoNumberSeedResponse
            {
                ResponseName = "SetAutoNumberSeed"
            };
        }
    }
}
#endif
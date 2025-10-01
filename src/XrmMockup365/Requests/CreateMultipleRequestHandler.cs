using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using DG.Tools.XrmMockup.Database;
using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup
{
    internal class CreateMultipleRequestHandler : RequestHandler
    {
        internal CreateMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, nameof(EventOperation.CreateMultiple))
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CreateMultipleRequest>(orgRequest);

            var ids =
                request.Targets.Entities.Select(entity =>
                {
                    var createRequest = new CreateRequest
                    {
                        Target = entity
                    };
                    var createResponse = core.Execute(createRequest, userRef) as CreateResponse;
                    return createResponse.id;
                }).ToArray();

            var response = new CreateMultipleResponse();
            response.Results["Ids"] = ids;
            return response;
        }
    }
}

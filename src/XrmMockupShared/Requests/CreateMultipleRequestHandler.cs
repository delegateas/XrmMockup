using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class CreateMultipleRequestHandler : RequestHandler
    {
        internal CreateMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, "CreateMultiple")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CreateMultipleRequest>(orgRequest);
            var response = new CreateMultipleResponse();
            var responses = new List<CreateResponseItem>();

            foreach (var createRequest in request.Requests)
            {
                var createResponse = core.Execute(createRequest, userRef) as CreateResponse;
                responses.Add(new CreateResponseItem
                {
                    RequestIndex = request.Requests.IndexOf(createRequest),
                    Response = createResponse
                });
            }

            response.Results["Responses"] = responses;
            return response;
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class UpsertMultipleRequestHandler : RequestHandler
    {
        internal UpsertMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, nameof(EventOperation.UpsertMultiple))
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpsertMultipleRequest>(orgRequest);

            var results = request.Targets.Entities.Select(entity =>
            {
                var upsertRequest = new UpsertRequest
                {
                    Target = entity
                };

                return core.Execute(upsertRequest, userRef);
            }).Cast<UpsertResponse>().ToArray();

            var response = new UpsertMultipleResponse();
            response["Results"] = results;

            return response;
        }
    }
}

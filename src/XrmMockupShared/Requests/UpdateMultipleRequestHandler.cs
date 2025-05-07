using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class UpdateMultipleRequestHandler : RequestHandler
    {
        internal UpdateMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, nameof(EventOperation.UpdateMultiple))
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpdateMultipleRequest>(orgRequest);

            var seenIds = new HashSet<Guid>();
            foreach (var entity in request.Targets.Entities)
            {
                if (seenIds.Contains(entity.Id))
                {
                    continue;
                }

                seenIds.Add(entity.Id);
                var updateRequest = new UpdateRequest
                {
                    Target = entity
                };

                core.Execute(updateRequest, userRef);
            }

            return new UpdateMultipleResponse();
        }
    }
}

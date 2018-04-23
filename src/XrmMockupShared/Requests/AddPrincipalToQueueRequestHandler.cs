#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace DG.Tools.XrmMockup
{
    internal class AddPrincipalToQueueRequestHandler : RequestHandler
    {
        internal AddPrincipalToQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "AddPrincipalToQueue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<AddPrincipalToQueueRequest>(orgRequest);

            if(request.Principal == null)
            {
                throw new FaultException("Required field 'Principal' is missing");
            }

            var principalMetadata = metadata.EntityMetadata.GetMetadata(request.Principal.LogicalName);

            if(principalMetadata == null)
            {
                throw new FaultException($"The entity with a name = '{request.Principal.LogicalName}' was not found in the MetadataCache.");
            }

            if(request.QueueId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var queue = db.GetDbRowOrNull(new EntityReference("queue", request.QueueId));

            if(queue == null)
            {
                throw new FaultException($"queue With Id = {request.QueueId} Does Not Exist");
            }

            if(request.Principal.LogicalName != "team" && request.Principal.LogicalName != "systemuser")
            {
                throw new FaultException("This action is not supported.");
            }

            var principal = db.GetDbRowOrNull(request.Principal.ToEntityReference());

            if(principal == null)
            {
                throw new FaultException("An unexpected error occurred.");
            }

            return new AddPrincipalToQueueResponse();
        }
    }
}
#endif
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace DG.Tools.XrmMockup
{
    internal class RouteToRequestHandler : RequestHandler
    {
        internal RouteToRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RouteTo")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RouteToRequest>(orgRequest);

            if (request.Target == null)
            {
                throw new FaultException("Required field 'Target' is misssing");
            }

            if (string.IsNullOrEmpty(request.Target.LogicalName))
            {
                throw new FaultException("Required member 'LogicalName' missing for field 'Target'");
            }

            var targetLogicalName = request.Target.LogicalName;

            var entityMetadata = metadata.EntityMetadata.GetMetadata(targetLogicalName);

            if (entityMetadata == null)
            {
                throw new FaultException($"The entity with a name = '{targetLogicalName}' with namemapping = 'Logical' was not found in the MetadataCache.");
            }

            if (request.QueueItemId.Equals(Guid.Empty) || request.Target.Id.Equals(Guid.Empty))
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var queueItem = db.GetEntityOrNull(new EntityReference(LogicalNames.QueueItem, request.QueueItemId));

            if (queueItem == null)
            {
                throw new FaultException($"{LogicalNames.QueueItem} With Id = {request.QueueItemId} Does Not Exist");
            }

            var targetRow = db.GetDbRowOrNull(request.Target);

            if (targetLogicalName == LogicalNames.Queue)
            {
                var addToQueueRequest = new AddToQueueRequest
                {
                    Target = queueItem["objectid"] as EntityReference,
                    SourceQueueId = queueItem.Id,
                    DestinationQueueId = request.Target.Id
                };
                core.Execute(addToQueueRequest as OrganizationRequest, userRef);
            }
            else if (targetLogicalName == LogicalNames.SystemUser || targetLogicalName == LogicalNames.Team)
            {
                var pickFromQueueRequest = new PickFromQueueRequest
                {
                    WorkerId = request.Target.Id,
                    QueueItemId = request.QueueItemId
                };
                core.Execute(pickFromQueueRequest as OrganizationRequest, userRef);
            }
            else
            {
                throw new FaultException("Invalid Route To Entity details provided.");
            }
           
            var response = new RouteToResponse();
            return response;
        }
    }
}
#endif
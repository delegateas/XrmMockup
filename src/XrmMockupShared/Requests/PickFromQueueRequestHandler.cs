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
    internal class PickFromQueueRequestHandler : RequestHandler
    {
        internal PickFromQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "PickFromQueue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<PickFromQueueRequest>(orgRequest);

            if(request.QueueItemId == Guid.Empty || request.WorkerId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var worker = db.GetEntityOrNull(new EntityReference(LogicalNames.SystemUser, request.WorkerId));

            if(worker == null)
            {
                throw new FaultException($"Invalid workerid: {request.WorkerId} of type 8");
            }

            var queueItem = db.GetEntityOrNull(new EntityReference(LogicalNames.QueueItem, request.QueueItemId));

            if(queueItem == null)
            {
                throw new FaultException($"{LogicalNames.QueueItem} With Id = {request.QueueItemId} Does Not Exist");
            }

            if(!security.HasPermission(queueItem, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException("You are not allowed to pick this item.");
            }

            queueItem["workerid"] = worker.ToEntityReference();
            db.Update(queueItem);

            return new PickFromQueueResponse();
        }
    }
}
#endif

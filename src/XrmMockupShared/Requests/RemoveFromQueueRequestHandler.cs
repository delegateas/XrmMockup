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
    internal class RemoveFromQueueRequestHandler : RequestHandler
    {
        internal RemoveFromQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RemoveFromQueue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RemoveFromQueueRequest>(orgRequest);

            if (request.QueueItemId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var queueItem = db.GetEntityOrNull(new EntityReference("queueitem", request.QueueItemId));

            if (queueItem == null)
            {
                throw new FaultException($"queueitem With Id = {request.QueueItemId} Does Not Exist");
            }

            if (!security.HasPermission(queueItem, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException("You are not allowed to remove this item.");
            }

            queueItem["queueid"] = null;
            db.Update(queueItem);

            return new RemoveFromQueueResponse();
        }
    }
}
#endif

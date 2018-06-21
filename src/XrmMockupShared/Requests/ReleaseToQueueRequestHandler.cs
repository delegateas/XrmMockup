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
    internal class ReleaseToQueueRequestHandler : RequestHandler
    {
        internal ReleaseToQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "ReleaseToQueue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<ReleaseToQueueRequest>(orgRequest);

            if(request.QueueItemId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var queueItem = db.GetEntityOrNull(new EntityReference("queueitem", request.QueueItemId));

            if(queueItem == null)
            {
                throw new FaultException($"queueitem With Id = {request.QueueItemId} Does Not Exist");
            }

            if (!security.HasPermission(queueItem, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException("You are not allowed to release this item.");
            }

            queueItem["workerid"] = null;
            db.Update(queueItem);

            return new ReleaseToQueueResponse();
        }
    }
}
#endif

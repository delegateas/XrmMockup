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
    internal class RetrieveUserQueuesRequestHandler : RequestHandler
    {
        internal RetrieveUserQueuesRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveUserQueues")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveUserQueuesRequest>(orgRequest);

            if (request.UserId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var queueMemberships =
                (from queue in db.GetDBEntityRows("queue")
                 where request.IncludePublic || queue.GetColumn<int>("queueviewtype") == 1
                 join membership in db.GetDBEntityRows("queuemembership") on queue.Id equals membership.GetColumn<Guid?>("queueid").Value
                 where membership.GetColumn<Guid?>("systemuserid").Value == request.UserId
                 select queue.ToEntity())
                .ToList();

            var queueCollection = new EntityCollection(queueMemberships)
            {
                EntityName = "queue"
            };

            var response = new RetrieveUserQueuesResponse();
            response.Results["EntityCollection"] = queueCollection;
            return response;
        }
    }
}
#endif

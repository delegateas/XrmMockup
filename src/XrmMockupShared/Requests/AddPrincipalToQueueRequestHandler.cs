#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.ServiceModel;

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

            var principalMetadata = metadata.EntityMetadata.GetMetadata(request.Principal.LogicalName) ?? 
                throw new FaultException($"The entity with a name = '{request.Principal.LogicalName}' was not found in the MetadataCache.");

            if(request.QueueId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            var queue = db.GetEntityOrNull(new EntityReference(LogicalNames.Queue, request.QueueId)) ??
                throw new FaultException($"{LogicalNames.Queue} With Id = {request.QueueId} Does Not Exist");

            if (request.Principal.LogicalName != LogicalNames.Team && request.Principal.LogicalName != LogicalNames.SystemUser)
            {
                throw new FaultException("This action is not supported.");
            }

            var principal = db.GetDbRowOrNull(request.Principal.ToEntityReference()) ?? 
                throw new FaultException($"{request.Principal.LogicalName} With Id = {request.Principal.Id} Does Not Exist");

            if(!security.HasPermission(queue, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException($"Principal user(Id={userRef.Id}, type=8) is missing prvWriteQueue");
            }

            var queueMembership = new Entity(LogicalNames.QueueMembership);
            queueMembership["systemuserid"] = principal.Id;
            queueMembership["queueid"] = queue.Id;
            db.Add(queueMembership);

            return new AddPrincipalToQueueResponse();
        }
    }
}
#endif
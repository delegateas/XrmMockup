using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace DG.Tools.XrmMockup
{
    internal class AddToQueueRequestHandler : RequestHandler
    {
        internal AddToQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "AddToQueue")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<AddToQueueRequest>(orgRequest);

            if (request.Target == null)
            {
                throw new FaultException("Required field 'Target' is missing");
            }

            var targetMetadata = metadata.EntityMetadata.GetMetadata(request.Target.LogicalName);

            if (targetMetadata == null)
            {
                throw new FaultException($"The entity with a name = '{request.Target.LogicalName}' was not found in the MetadataCache.");
            }

            if (request.DestinationQueueId == Guid.Empty)
            {
                throw new FaultException("Expected non-empty Guid.");
            }

            if (targetMetadata.IsValidForQueue == null || !targetMetadata.IsValidForQueue.Value)
            {
                throw new FaultException("This object type cannot be added to a queue.");
            }
            
            var queueItemRow = db.GetDBEntityRows(LogicalNames.QueueItem)
                .FirstOrDefault(r =>
                    r.GetColumn<DbRow>("objectid")?.Id == request.Target.Id &&
                    r.GetColumn<DbRow>("queueid")?.Id == (request.SourceQueueId != Guid.Empty ? request.SourceQueueId : request.DestinationQueueId));

            Entity source = null;
            if (request.SourceQueueId != Guid.Empty)
            {
                source = db.GetEntityOrNull(new EntityReference(LogicalNames.Queue, request.SourceQueueId));
            }

            var target = db.GetEntityOrNull(request.Target);

            if (request.SourceQueueId != Guid.Empty && (queueItemRow == null || source == null))
            {
                throw new FaultException($"Could not find any queue item associated with the Target with id: {request.Target.Id} in the specified SourceQueueId: {request.SourceQueueId}. Either the SourceQueueId or Target is invalid or the queue item does not exist.");
            }
            else if (queueItemRow == null && target == null)
            {
                throw new FaultException($"{request.Target.LogicalName} With Id = {request.Target.Id} Does Not Exist");
            }

            var destination = db.GetEntityOrNull(new EntityReference(LogicalNames.Queue, request.DestinationQueueId));

            if (destination == null)
            {
                throw new FaultException($"DestinationQueueId {request.Target.Id} does not exist.");
            }

            if (request.QueueItemProperties != null)
            {
                var propertiesMetadata = metadata.EntityMetadata.GetMetadata(request.QueueItemProperties.LogicalName);

                if (propertiesMetadata == null)
                {
                    throw new FaultException($"The entity with a name = '{request.QueueItemProperties.LogicalName}' was not found in the MetadataCache.");
                }

                if (propertiesMetadata.LogicalName != LogicalNames.QueueItem)
                {
                    throw new FaultException("An unexpected error occured.");
                }
                var attributeLogicalNames = propertiesMetadata.Attributes.Select(a => a.LogicalName).ToList();

                var unsupportedAttribute = request
                    .QueueItemProperties
                    .Attributes
                    .Select(kv => kv.Key)
                    .FirstOrDefault(key => String.IsNullOrEmpty(key) || !attributeLogicalNames.Contains(key));

                if (unsupportedAttribute != null)
                {
                    throw new FaultException($"Invalid EntityKey Operation performed : Entity {propertiesMetadata.LogicalName} does not contain an attribute named {unsupportedAttribute}");
                }
            }

            bool hasTargetPermission = security.HasPermission(target, AccessRights.ReadAccess, userRef);

            bool hasQueueItemPermission = security.HasPermission(new Entity(LogicalNames.QueueItem), AccessRights.None, userRef);

            bool hasQueuePermission = security.HasPermission(destination, AccessRights.ReadAccess, userRef) &&
                (request.SourceQueueId == Guid.Empty || security.HasPermission(source, AccessRights.ReadAccess, userRef));

            if (!hasQueuePermission || !hasTargetPermission || !hasQueueItemPermission)
            {
                throw new FaultException($"Principal user (Id={userRef.Id}, type=8) is missing prvReadQueue privilege (Id=b140e729-dfeb-4ba1-a33f-39ff830bac90)");
            }

            var queueItem = queueItemRow?.ToEntity();
            if (queueItem == null)
            {

                queueItem = request.QueueItemProperties != null ? Utility.CloneEntity(request.QueueItemProperties) : new Entity(LogicalNames.QueueItem);
                queueItem.Id = Guid.Empty;
                queueItem["queueid"] = destination.ToEntityReference();
                var createQueueItemRequest = new CreateRequest
                {
                    Target = queueItem
                };
                queueItem.Id = (core.Execute(createQueueItemRequest as OrganizationRequest, userRef) as CreateResponse).id;
            }
            else
            {
                queueItem["queueid"] = destination.ToEntityReference();
                db.Update(queueItem);
            }

            var response = new AddToQueueResponse();
            response["QueueItemId"] = queueItem.Id;
            return response;
        }
    }
}
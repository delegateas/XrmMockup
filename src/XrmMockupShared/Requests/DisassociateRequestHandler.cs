using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup {
    internal class DisassociateRequestHandler : RequestHandler {
        internal DisassociateRequestHandler(Core core, IXrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Disassociate") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<DisassociateRequest>(orgRequest);
            var relatedLogicalName = request.RelatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as ManyToManyRelationshipMetadata;

            var targetEntity = db.GetEntityOrNull(request.Target);

            if (targetEntity == null)
                return new DisassociateResponse();

            if (!security.HasPermission(targetEntity, AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Trying to unappend to entity '{request.Target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!security.HasPermission(targetEntity, AccessRights.AppendToAccess, userRef)) {
                throw new FaultException($"Trying to unappend to entity '{request.Target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }
            foreach (var r in request.RelatedEntities)
            {
                var entity = db.GetEntityOrNull(r);
                if (entity != null && !security.HasPermission(entity, AccessRights.ReadAccess, userRef))
                {
                    throw new FaultException($"Trying to unappend entity '{entity.LogicalName}'" +
                        $" to '{request.Target.LogicalName}', but the calling user does not have read access for that entity");
                }

                if (entity != null && !security.HasPermission(entity, AccessRights.AppendAccess, userRef))
                {
                    throw new FaultException($"Trying to unappend entity '{entity.LogicalName}'" +
                        $" to '{request.Target.LogicalName}', but the calling user does not have append access for that entity");
                }
            }

            if (manyToMany != null) {
                foreach (var relatedEntity in request.RelatedEntities) {
                    if (request.Target.LogicalName == manyToMany.Entity1LogicalName) {
                        var link = db.GetEntities(manyToMany.IntersectEntityName)
                            .First(row =>
                                row.GetAttributeValue<Guid>(manyToMany.Entity1IntersectAttribute) == request.Target.Id &&
                                row.GetAttributeValue<Guid>(manyToMany.Entity2IntersectAttribute) == relatedEntity.Id
                            );

                        db.Delete(link);
                    } else {
                        var link = db.GetEntities(manyToMany.IntersectEntityName)
                            .First(row =>
                                row.GetAttributeValue<Guid>(manyToMany.Entity1IntersectAttribute) == relatedEntity.Id &&
                                row.GetAttributeValue<Guid>(manyToMany.Entity2IntersectAttribute) == request.Target.Id
                            );
                        db.Delete(link);
                    }
                }
            } else {
                if (oneToMany.ReferencedEntity == request.Target.LogicalName) {
                    foreach (var relatedEntity in request.RelatedEntities) {
                        var dbEntity = db.GetEntityOrNull(relatedEntity);
                        if (dbEntity != null)
                        {
                            dbEntity[oneToMany.ReferencingAttribute] = null;
                            Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
                            db.Update(dbEntity);
                        }
                    }
                } else {
                    if (request.RelatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (request.RelatedEntities.Count == 1) {
                        var related = request.RelatedEntities.First();
                        var dbEntity = db.GetEntityOrNull(request.Target);
                        if (dbEntity != null)
                        {
                            dbEntity[oneToMany.ReferencingAttribute] = null;
                            Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
                            db.Update(dbEntity);
                        }
                    }
                }
            }


            return new DisassociateResponse();
        }
    }
}

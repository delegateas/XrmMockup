﻿using System;
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
    internal class AssociateRequestHandler : RequestHandler {
        internal AssociateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, DataMethods datamethods) : base(core, db, metadata, datamethods, "Associate") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<AssociateRequest>(orgRequest);
            var relatedLogicalName = request.RelatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as ManyToManyRelationshipMetadata;

            var targetEntity = db.GetEntity(request.Target);
            if (!dataMethods.HasPermission(targetEntity, AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Trying to append to entity '{request.Target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!dataMethods.HasPermission(targetEntity, AccessRights.AppendToAccess, userRef)) {
                throw new FaultException($"Trying to append to entity '{request.Target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }

            if (request.RelatedEntities.Any(r => !dataMethods.HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef))) {
                var firstError = request.RelatedEntities.First(r => !dataMethods.HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef));
                throw new FaultException($"Trying to append entity '{firstError.LogicalName}'" +
                    $" to '{request.Target.LogicalName}', but the calling user does not have read access for that entity");
            }

            if (request.RelatedEntities.Any(r => !dataMethods.HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef))) {
                var firstError = request.RelatedEntities.First(r => !dataMethods.HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef));
                throw new FaultException($"Trying to append entity '{firstError.LogicalName}'" +
                    $" to '{request.Target.LogicalName}', but the calling user does not have append access for that entity");
            }

            if (manyToMany != null) {
                foreach (var relatedEntity in request.RelatedEntities) {
                    var linker = new Entity(manyToMany.IntersectEntityName) {
                        Id = Guid.NewGuid()
                    };
                    if (request.Target.LogicalName == manyToMany.Entity1LogicalName) {
                        linker.Attributes[manyToMany.Entity1IntersectAttribute] = request.Target.Id;
                        linker.Attributes[manyToMany.Entity2IntersectAttribute] = relatedEntity.Id;
                    } else {
                        linker.Attributes[manyToMany.Entity1IntersectAttribute] = relatedEntity.Id;
                        linker.Attributes[manyToMany.Entity2IntersectAttribute] = request.Target.Id;
                    }
                    db.Add(linker);
                }
            } else {
                if (oneToMany.ReferencedEntity == request.Target.LogicalName) {
                    foreach (var relatedEntity in request.RelatedEntities) {
                        var dbEntity = db.GetEntity(relatedEntity);
                        dbEntity[oneToMany.ReferencingAttribute] = request.Target;
                        Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
                        db.Update(dbEntity);
                    }
                } else {
                    if (request.RelatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (request.RelatedEntities.Count == 1) {
                        var related = request.RelatedEntities.First();
                        var dbEntity = db.GetEntity(request.Target);
                        dbEntity[oneToMany.ReferencingAttribute] = related;
                        Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
                        db.Update(dbEntity);
                    }
                }
            }
            return new AssociateResponse();
        }
    }
}

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
    internal class AssignRequestHandler : RequestHandler {
        internal AssignRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, DataMethods datamethods) : base(core, db, metadata, datamethods, "Assign") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<AssignRequest>(orgRequest);
            var dbEntity = core.GetDbEntityWithRelatedEntities(request.Target, EntityRole.Referenced, userRef);
            dataMethods.CheckAssignPermission(dbEntity, request.Assignee, userRef);

            // Cascade
            foreach (var relatedEntities in dbEntity.RelatedEntities) {
                var relationshipMeta = metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName).OneToManyRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                var req = new AssignRequest();
                switch (relationshipMeta.CascadeConfiguration.Assign) {
                    case CascadeType.Cascade:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            req.Target = relatedEntity.ToEntityReference();
                            req.Assignee = request.Assignee;
                            core.Execute(req, userRef, null);
                        }
                        break;
                    case CascadeType.Active:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            if (db.GetEntity(relatedEntity.ToEntityReference()).GetAttributeValue<OptionSetValue>("statecode")?.Value == 0) {
                                req.Target = relatedEntity.ToEntityReference();
                                req.Assignee = request.Assignee;
                                core.Execute(req, userRef, null);
                            }
                        }
                        break;
                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if (db.GetEntity(relatedEntity.ToEntityReference()).GetAttributeValue<EntityReference>("ownerid")?.Id == currentOwner.Id) {
                                req.Target = relatedEntity.ToEntityReference();
                                req.Assignee = request.Assignee;
                                core.Execute(req, userRef, null);
                            }
                        }
                        break;
                }
            }
            Utility.SetOwner(db, dataMethods, metadata, dbEntity, request.Assignee);
            Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
            return new AssignResponse();
        }
    }
}

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
    internal class DeleteRequestHandler : RequestHandler {
        internal DeleteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, DataMethods datamethods) : base(core, db, metadata, datamethods, "Delete") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<DeleteRequest>(orgRequest);
            var entity = core.GetDbEntityWithRelatedEntities(request.Target, EntityRole.Referenced, userRef);
            if (entity == null) {
                throw new FaultException($"{request.Target.LogicalName} with Id '{request.Target.Id}' does not exist");
            }

            if (!dataMethods.HasPermission(entity, AccessRights.DeleteAccess, userRef)) {
                throw new FaultException($"You do not have permission to access entity '{request.Target.LogicalName}' for delete");
            }

            if (entity != null) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    var relationshipMeta = metadata.EntityMetadata.GetMetadata(entity.LogicalName).OneToManyRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                    switch (relationshipMeta.CascadeConfiguration.Assign) {
                        case CascadeType.Cascade:
                            foreach (var relatedEntity in relatedEntities.Value.Entities) {
                                var req = new DeleteRequest {
                                    Target = new EntityReference(relatedEntity.LogicalName, relatedEntity.Id)
                                };
                                core.Execute(req, userRef, null);
                            }
                            break;
                        case CascadeType.RemoveLink:
                            var disassocHandler = core.RequestHandlers.Find(x => x is DisassociateRequestHandler);
                            disassocHandler.Execute(new DisassociateRequest {
                                RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()),
                                Relationship = relatedEntities.Key,
                                Target = entity.ToEntityReference()
                            }, userRef);
                            break;
                        case CascadeType.Restrict:
                            var trace = core.ServiceFactory.GetService(typeof(ITracingService)) as ITracingService;
                            trace.Trace($"Delete restricted for {relatedEntities.Key.SchemaName} when trying to delete {entity.LogicalName} with id {entity.Id}");
                            return new DeleteResponse();
                    }
                }

                db[entity.LogicalName].Remove(entity.Id);
            }
            return new DeleteResponse();
        }
    }
}

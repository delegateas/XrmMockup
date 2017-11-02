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
    internal class RetrieveEntityRequestHandler : RequestHandler {
        internal RetrieveEntityRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveEntity") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveEntityRequest>(orgRequest);

            if (request.LogicalName == null && request.MetadataId== Guid.Empty) {
                throw new FaultException("Entity logical name is required when MetadataId is not specified");
            }

            EntityMetadata entityMetadata = null;

            if (request.LogicalName != null && metadata.EntityMetadata.ContainsKey(request.LogicalName)) {
                entityMetadata = metadata.EntityMetadata[request.LogicalName];
            } else if (request.MetadataId != Guid.Empty) {
                entityMetadata = metadata.EntityMetadata.FirstOrDefault(x => x.Value.MetadataId == request.MetadataId).Value;
            } else {
                throw new FaultException($"Could not find entity with logicalname {request.LogicalName} or metadataid {request.MetadataId}");
            }

            var resp = new RetrieveEntityResponse();
            resp.Results["EntityMetadata"] = entityMetadata;
            return resp;
        }
    }
}

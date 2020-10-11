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

namespace DG.Tools.XrmMockup
{
    internal class RetrieveMetadataChangesRequestHandler : RequestHandler
    {
        internal RetrieveMetadataChangesRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveMetadataChanges") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveMetadataChangesRequest>(orgRequest);

            var q = request.Query;
            var c = q.Criteria.Conditions.First();

            var m = metadata.EntityMetadata.Where(x => x.Value.ObjectTypeCode.Value == (int)c.Value);

            var resp = new RetrieveMetadataChangesResponse();
            var col = new EntityMetadataCollection();
            col.Add(m.Single().Value);
            resp.Results["EntityMetadata"] = col;

            return resp;
        }


    }
}

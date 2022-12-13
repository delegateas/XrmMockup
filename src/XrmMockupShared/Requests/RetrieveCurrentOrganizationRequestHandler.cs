using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveCurrentOrganizationRequestHandler : RequestHandler
    {
        internal RetrieveCurrentOrganizationRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveCurrentOrganization") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveCurrentOrganizationRequest>(orgRequest);

            var response = new RetrieveCurrentOrganizationResponse();
            response.Results["Detail"] = core.orgDetail;

            return response;
        }
    }
}

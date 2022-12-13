using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveCurrentOrganizationRequestHandler : RequestHandler
    {
        internal RetrieveCurrentOrganizationRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata,
            Security security) : base(core, db, metadata, security,
            "RetrieveCurrentOrganization")

        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            var response = new RetrieveCurrentOrganizationResponse();
            response.Results["Detail"] = core.orgDetail;
            return response;
#else
            return new OrganizationResponse();
#endif
        }
    }
}
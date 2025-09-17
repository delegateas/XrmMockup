using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class CloseQuoteRequestHandler : RequestHandler
    {
        internal CloseQuoteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CloseQuote") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CloseQuoteRequest>(orgRequest);
            Utility.CloseQuote(core, QuoteState.Closed, request.Status, request.QuoteClose, userRef);
            return new CloseQuoteResponse();
        }
    }
}
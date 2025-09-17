using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class WinQuoteRequestHandler : RequestHandler
    {
        internal WinQuoteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "WinQuote") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<WinQuoteRequest>(orgRequest);
            Utility.CloseQuote(core, QuoteState.Won, request.Status, request.QuoteClose, userRef);
            return new WinQuoteResponse();
        }
    }
}
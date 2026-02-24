using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class ExecuteTransactionRequestHandler : RequestHandler
    {
        internal ExecuteTransactionRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "ExecuteTransaction") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<ExecuteTransactionRequest>(orgRequest);
            var toReturn = new ExecuteTransactionResponse();
            toReturn.Results["Responses"] = new OrganizationResponseCollection();

            var snapshot = core.TakeJsonSnapshot();

            try
            {
                foreach (var req in request.Requests)
                {
                    toReturn.Responses.Add(core.Execute(req, userRef));
                }

                return toReturn;
            }
            catch (Exception)
            {
                core.RestoreJsonSnapshot(snapshot);
                throw;
            }
        }
    }
}

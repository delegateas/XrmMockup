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
            catch (Exception e)
            {
                core.RestoreJsonSnapshot(snapshot);
                throw;
            }
        }
    }
}

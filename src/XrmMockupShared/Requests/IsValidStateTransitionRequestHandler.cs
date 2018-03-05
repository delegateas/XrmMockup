#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
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
using System.Xml.Linq;

namespace DG.Tools.XrmMockup
{
    internal class IsValidStateTransitionRequestHandler : RequestHandler
    {
        internal IsValidStateTransitionRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "IsValidStateTransition") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<IsValidStateTransitionRequest>(orgRequest);

            if (request.Entity == null)
            {
                throw new FaultException("Required field 'Entity' is missing");
            }
            if (request.NewState == null)
            {
                throw new FaultException("Required field 'NewState' is missing");
            }
            var row = db.GetDbRowOrNull(request.Entity);
            if (row == null)
            {
                throw new FaultException($"{request.Entity.LogicalName} With Id = {request.Entity.Id} Does Not Exist");
            }


            var newStatusCode = request.NewStatus;
            var newStateCode = request.NewState;
            var prevStatusCode = row["statuscode"] as OptionSetValue;

            var entityMetadata = metadata.EntityMetadata.GetMetadata(request.Entity.LogicalName);
            if (entityMetadata.EnforceStateTransitions != true)
            {
                throw new FaultException($"This message can not be used to check the state transition of {request.Entity.LogicalName} to {request.NewStatus}.");
            }

            var stateOptionMeta = (entityMetadata.Attributes
                .FirstOrDefault(a => a is StateAttributeMetadata) as StateAttributeMetadata)
                .OptionSet.Options;
            // Should be changed to InvariantName somehow
            if (!stateOptionMeta.Any(o => o.Value.ToString() == newStateCode))
            {
                throw new FaultException($"-1 is not a valid state code on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
            }

            var statusOptionMeta = (entityMetadata.Attributes
                .FirstOrDefault(a => a is StatusAttributeMetadata) as StatusAttributeMetadata)
                .OptionSet.Options;
            if (!statusOptionMeta.Any(o => o.Value == newStatusCode))
            {
                throw new FaultException($"{request.NewStatus} is not a valid status code on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
            }

            var prevValueOptionMeta = statusOptionMeta.FirstOrDefault(o => o.Value == prevStatusCode.Value) as StatusOptionMetadata;

            var transitions = prevValueOptionMeta.TransitionData;
            var resp = new IsValidStateTransitionResponse();
            if (transitions != null && transitions != "")
            {
                var ns = XNamespace.Get("http://schemas.microsoft.com/crm/2009/WebServices");
                var doc = XDocument.Parse(transitions).Element(ns + "allowedtransitions");
                if (doc.Descendants(ns + "allowedtransition")
                    .Where(x => x.Attribute("tostatusid").Value == prevStatusCode.Value.ToString())
                    .Any())
                {

                }
            }
            resp.Results["IsValid"] = null;
            return resp;
        }
    }
}
#endif
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

            var entityMetadata = metadata.EntityMetadata.GetMetadata(request.Entity.LogicalName);

            var row = db.GetDbRowOrNull(request.Entity);
            if (row == null)
            {
                throw new FaultException($"{request.Entity.LogicalName} With Id = {request.Entity.Id} Does Not Exist");
            }

            var prevStatusCode = row.GetColumn<int>("statuscode");

            if (entityMetadata.IsStateModelAware != true)
            {
                throw new FaultException($"The 'IsValidStateTransition' method does not support entities of type {request.Entity.LogicalName}.");
            }
            var stateOptionMeta = (entityMetadata.Attributes
                .FirstOrDefault(a => a is StateAttributeMetadata) as StateAttributeMetadata)
                .OptionSet
                .Options;
            
            var stateOption = stateOptionMeta.FirstOrDefault(o => (o as StateOptionMetadata).InvariantName == request.NewState);

            if (entityMetadata.EnforceStateTransitions != true)
            {
                var tryState = stateOption != null ? stateOption.Value.ToString() : "-1";
                throw new FaultException($"This message can not be used to check the state transition of {request.Entity.LogicalName} to {tryState}");
            }
            if (stateOption == null)
            {
                throw new FaultException($"-1 is not a valid state code on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
            }

            var statusOptionMeta = (entityMetadata.Attributes
                .FirstOrDefault(a => a is StatusAttributeMetadata) as StatusAttributeMetadata)
                .OptionSet.Options;
            if (!statusOptionMeta.Any(o => 
                (o as StatusOptionMetadata).State == stateOption.Value 
                    && o.Value == request.NewStatus 
                    && stateOption.Value != prevStatusCode))
            {
                throw new FaultException($"{request.NewStatus} is not a valid status code on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
            }

            var prevValueOptionMeta = statusOptionMeta.FirstOrDefault(o => o.Value == prevStatusCode) as StatusOptionMetadata;

            var transitions = prevValueOptionMeta.TransitionData;
            var resp = new IsValidStateTransitionResponse();
            if (transitions != null && transitions != "")
            {
                var ns = XNamespace.Get("http://schemas.microsoft.com/crm/2009/WebServices");
                var doc = XDocument.Parse(transitions).Element(ns + "allowedtransitions");
                if (doc.Descendants(ns + "allowedtransition")
                    .Where(x => x.Attribute("tostatusid").Value == request.NewStatus.ToString())
                    .Any())
                {
                    resp.Results["IsValid"] = true;
                }
                else
                {
                    throw new FaultException($"{request.NewStatus} is not a valid status code for state code {request.Entity.LogicalName}State.{request.NewState} on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
                }
            }
            else
            {
                resp.Results["IsValid"] = null;
            }
            return resp;
        }
    }
}
#endif
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
    internal class CloseIncidentRequestHandler : RequestHandler
    {
        internal CloseIncidentRequestHandler(Core core, IXrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CloseIncident") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {

            var request = MakeRequest<CloseIncidentRequest>(orgRequest);

            if (request.Status == null)
            {
                throw new FaultException("Required field 'Status' is missing");
            }

            if (request.IncidentResolution == null)
            {
                throw new FaultException("Incident resolution is missing");
            }

            if (request.IncidentResolution.LogicalName == null)
            {
                throw new FaultException("Required member 'LogicalName' missing for field 'IncidentResolution'");
            }

            if (request.IncidentResolution.LogicalName != "incidentresolution")
            {
                throw new FaultException("An unexpected error occurred.");
            }

            if (!metadata.EntityMetadata.ContainsKey(request.IncidentResolution.LogicalName))
            {
                throw new FaultException($"The entity with a name = '{request.IncidentResolution.LogicalName}' was not found in the MetadataCache.");
            }

            var incidentRef = request.IncidentResolution.GetAttributeValue<EntityReference>("incidentid");

            if (incidentRef == null)
            {
                throw new FaultException("The incident id is missing.");
            }

            var entityMetadata = metadata.EntityMetadata.GetMetadata("incident");
            var statusOptionMeta = Utility.GetStatusOptionMetadata(entityMetadata);

            if (!statusOptionMeta.Any(o => (o as StatusOptionMetadata).Value == request.Status.Value
                && (o as StatusOptionMetadata).State == 1))
            {
                throw new FaultException($"{request.Status.Value} is not a valid status code on incident with Id {incidentRef.Id}.");
            }

            var incident = db.GetEntity(incidentRef);

            if (incident == null)
            {
                throw new FaultException($"incident With Id = {incidentRef.Id} Does Not Exist");
            }
            var incidentState = incident["statecode"] as OptionSetValue;

            if (incidentState.Value == 1)
            {
                throw new FaultException($"incident With Id = {incidentRef.Id} is resolved and connot be closed");
            }

            if (incidentState.Value == 2)
            {
                throw new FaultException($"incident With Id = {incidentRef.Id} is allready cancelled and connot be closed");
            }

            var setStaterequest = new SetStateRequest();
            setStaterequest.EntityMoniker = incidentRef;
            setStaterequest.State = new OptionSetValue(1);
            setStaterequest.Status = request.Status;
            core.Execute(setStaterequest, userRef);

            var incidentResolution = db.GetEntityOrNull(request.IncidentResolution.ToEntityReference());

            if (incidentResolution != null)
            {
                throw new FaultException("Cannot insert duplicate key.");
            }

            db.Add(request.IncidentResolution);

            return new CloseIncidentResponse();
        }
    }
}
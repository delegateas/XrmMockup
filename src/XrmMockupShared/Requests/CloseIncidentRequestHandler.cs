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
        internal CloseIncidentRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CloseIncident") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CloseIncidentRequest>(orgRequest);

            if (request.IncidentResolution == null)
            {
                throw new FaultException("Required field 'IncidentResolution' is missing");
            }

            if (request.IncidentResolution.LogicalName == null)
            {
                throw new FaultException("Required member 'LogicalName' missing for field 'IncidentResolution'");
            }

            if (!metadata.EntityMetadata.ContainsKey(request.IncidentResolution.LogicalName))
            {
                throw new FaultException($"The entity with a name = '{request.IncidentResolution.LogicalName}' was not found in the MetadataCache.");
            }

            var entityMetadata = metadata.EntityMetadata.GetMetadata(request.IncidentResolution.LogicalName);

            if (request.Status == null)
            {
                throw new FaultException("Required field 'Status' is missing");
            }

            if (request.IncidentResolution.LogicalName != "incidentresolution")
            {
                throw new FaultException("An unexpected error occurred.");
            }

            var incidentResolution = (IncidentResolution)request.IncidentResolution;
            
            var statusOptionMeta = Utility.GetStatusOptionMetadata(entityMetadata);

            if (!statusOptionMeta.Any(o => (o as StatusOptionMetadata).Value == request.Status.Value
                && (o as StatusOptionMetadata).State == 1))
            {
                throw new FaultException($"{request.Status} is not a valid status code on incident with Id {request.IncidentResolution.Id}.");
            }

            if(incidentResolution.IncidentId == null)
            {
                throw new FaultException("The incident id is missing.");
            }


            var resp = new CloseIncidentResponse();
            return resp;
        }
    }

    internal class IncidentResolution : Entity
    {
        public string Subject { get; set; }
        public EntityReference IncidentId { get; set; }
    }
}

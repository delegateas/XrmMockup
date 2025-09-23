using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class InstantiateTemplateRequestHandler : RequestHandler
    {
        public InstantiateTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "InstantiateTemplate") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InstantiateTemplateRequest>(orgRequest);

            if (request.TemplateId == Guid.Empty)
                throw new FaultException("Template id should be set.");

            if (request.ObjectId == Guid.Empty)
                throw new FaultException("Object id should be set.");

            if (string.IsNullOrEmpty(request.ObjectType))
                throw new FaultException("ObjectType is missing");

            var entity = new Entity("email");

            var collection = new EntityCollection();
            collection.Entities.Add(entity);

            var parameters = new ParameterCollection
            {
                { "EntityCollection", collection }
            };

            return new InstantiateTemplateResponse
            {
                Results = parameters,
                ResponseName = "InstantiateTemplate",
            };
        }
    }
}
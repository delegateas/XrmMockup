using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class InstantiateTemplateRequestHandler : RequestHandler
    {
        public InstantiateTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "InstantiateTemplate") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InstantiateTemplateRequest>(orgRequest);

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
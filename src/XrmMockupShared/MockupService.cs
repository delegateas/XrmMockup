using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using System.Reflection;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Plugin;

namespace DG.Tools.XrmMockup {

    /// <summary>
    /// A class for Mocking the IOrganizationService
    /// </summary>
    internal partial class MockupService : IOrganizationService {

        private Core core;
        private Guid userId;
        private EntityReference userRef;
        private MockupPluginContext pluginContext;
        private MockupServiceSettings settings;

        internal MockupService(Core core, Guid? userId, MockupPluginContext pluginContext, MockupServiceSettings settings) {
            this.core = core;
            this.pluginContext = pluginContext;
            this.userId = userId.GetValueOrDefault();
            if (userId.HasValue && userId.Value != Guid.Empty) {
                userRef = new EntityReference("systemuser", userId.Value);
            }
            this.settings = settings;
        }

        internal MockupService(Core core, Guid? userId, MockupPluginContext pluginContext) : this(core, userId, pluginContext, null) { }

        /// <summary>
        /// Create a new MockupService for the given Mockup of CRM 
        /// </summary>
        /// <param name="core"></param>
        public MockupService(Core core) : this(core, null, null, null) { }

        public MockupService(Core core, MockupServiceSettings settings) : this(core, null, null, settings) { }

        /// <summary>
        /// Associate the described entity with the relatedEntities
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="entityId"></param>
        /// <param name="relationship"></param>
        /// <param name="relatedEntities"></param>
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) {
            var req = new AssociateRequest();
            req.Target = new EntityReference { LogicalName = entityName, Id = entityId };
            req.Relationship = relationship;
            req.RelatedEntities = relatedEntities;
            SendRequest<AssociateResponse>(req);
        }

        /// <summary>
        /// Create a new instance of an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Guid Create(Entity entity) {
            var req = new CreateRequest();
            req.Target = entity;
            var resp = SendRequest<CreateResponse>(req);
            return resp.id;
        }

        /// <summary>
        /// Delete an instance of an entity
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        public void Delete(string entityName, Guid id) {
            var req = new DeleteRequest();
            req.Target = new EntityReference(entityName, id);
            SendRequest<DeleteResponse>(req);
        }

        /// <summary>
        /// Disassociate the described entity with the relatedEntities
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="entityId"></param>
        /// <param name="relationship"></param>
        /// <param name="relatedEntities"></param>
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) {
            var req = new DisassociateRequest();
            req.Target = new EntityReference { LogicalName = entityName, Id = entityId };
            req.Relationship = relationship;
            req.RelatedEntities = relatedEntities;
            SendRequest<DisassociateResponse>(req);
        }

        /// <summary>
        /// Get the described entity
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <param name="columnSet"></param>
        /// <returns></returns>
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) {
            var req = new RetrieveRequest();
            req.Target = new EntityReference(entityName, id);
            req.ColumnSet = columnSet;
            var resp = SendRequest<RetrieveResponse>(req);
            return resp.Entity;
        }

        /// <summary>
        /// Get the result of the query as an EntityCollection
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public EntityCollection RetrieveMultiple(QueryBase query) {
            var req = new RetrieveMultipleRequest();
            req.Query = query;
            var resp = SendRequest<RetrieveMultipleResponse>(req);
            return resp.EntityCollection;
        }

        /// <summary>
        /// Update the given entity
        /// </summary>
        /// <param name="entity"></param>
        public void Update(Entity entity) {
            var req = new UpdateRequest();
            req.Target = entity;
            SendRequest<UpdateResponse>(req);
        }

        /// <summary>
        /// Execute the given OrganizationRequest
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public OrganizationResponse Execute(OrganizationRequest request) {
            return SendRequest<OrganizationResponse>(request);
        }

        private T SendRequest<T>(OrganizationRequest request) where T : OrganizationResponse {
            MockupExecutionContext.SetSettings(request, settings);
            return (T)core.Execute(request, userRef ?? core.AdminUserRef, pluginContext);
        }

    }



}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

using System.Reflection;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using WorkflowExecuter;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk.Client;

namespace DG.Tools.XrmMockup {

    internal class Snapshot
    {
        public XrmDb db;
        public Security security;
        public EntityReference AdminUserRef;
        public EntityReference RootBusinessUnitRef;
        public Guid OrganizationId;
        public TimeSpan TimeOffset;
    }

    /// <summary>
    /// Class for handling all requests to the database
    /// </summary>
    internal class Core {

        private PluginManager pluginManager;
        private WorkflowManager workflowManager;
        private Security security;
        private XrmMockupSettings settings;
        private MetadataSkeleton metadata;
        private XrmDb db;
        private Dictionary<string, Snapshot> snapshots;
        private Dictionary<string, Type> entityTypeMap = new Dictionary<string, Type>();
        private OrganizationServiceProxy OnlineProxy;
        internal EntityReference baseCurrency;
        private int baseCurrencyPrecision;
        public TimeSpan TimeOffset { get; private set; }
        public MockupServiceProviderAndFactory ServiceFactory { get; }

        public EntityReference AdminUserRef;
        public EntityReference RootBusinessUnitRef;

        public List<RequestHandler> RequestHandlers;

        /// <summary>
        /// Organization id for the Mockup instance
        /// </summary>
        public Guid OrganizationId { get; private set; }

        /// <summary>
        /// Organization name for the Mockup instance
        /// </summary>
        public string OrganizationName { get; private set; }


        /// <summary>
        /// Creates a new instance of Core
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="metadata"></param>
        /// <param name="SecurityRoles"></param>
        /// <param name="Workflows"></param>
        public Core(XrmMockupSettings Settings, MetadataSkeleton metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) {
            this.TimeOffset = new TimeSpan();
            this.settings = Settings;
            this.metadata = metadata;
            baseCurrency = metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
            baseCurrencyPrecision = metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision");

            this.db = new XrmDb(metadata.EntityMetadata, GetOnlineProxy());
            this.snapshots = new Dictionary<string, Snapshot>();
            this.security = new Security(this, metadata, SecurityRoles);
            this.ServiceFactory = new MockupServiceProviderAndFactory(this);
            this.pluginManager = new PluginManager(Settings.BasePluginTypes, metadata.EntityMetadata, metadata.Plugins);
            this.workflowManager = new WorkflowManager(Settings.CodeActivityInstanceTypes, Settings.IncludeAllWorkflows, Workflows, metadata.EntityMetadata);

            this.RequestHandlers = GetRequestHandlers(db);
            InitializeDB();
            this.security.InitializeSecurityRoles(db);
        }

        private void InitializeDB() {
            this.OrganizationId = Guid.NewGuid();
            this.OrganizationName = "MockupOrganization";

            // Setup currencies
            var currencies = new List<Entity>();
            foreach (var entity in metadata.Currencies) {
                Utility.RemoveAttribute(entity, "createdby", "modifiedby", "organizationid", "modifiedonbehalfby", "createdonbehalfby");
                currencies.Add(entity);
            }
            this.db.AddRange(currencies);

            // Setup root business unit
            var rootBu = metadata.RootBusinessUnit;
            rootBu["name"] = "RootBusinessUnit";
            rootBu.Attributes.Remove("organizationid");
            this.db.Add(rootBu, false);
            this.RootBusinessUnitRef = rootBu.ToEntityReference();

            // Setup admin user
            var admin = new Entity(LogicalNames.SystemUser) {
                Id = Guid.NewGuid()
            };
            this.AdminUserRef = admin.ToEntityReference();

            admin["firstname"] = "";
            admin["lastname"] = "SYSTEM";
            admin["businessunitid"] = RootBusinessUnitRef;
            this.db.Add(admin);
        }

        private List<RequestHandler> GetRequestHandlers(XrmDb db) => new List<RequestHandler> {
                new CreateRequestHandler(this, db, metadata, security),
                new UpdateRequestHandler(this, db, metadata, security),
                new RetrieveMultipleRequestHandler(this, db, metadata, security),
                new RetrieveRequestHandler(this, db, metadata, security),
                new DeleteRequestHandler(this, db, metadata, security),
                new SetStateRequestHandler(this, db, metadata, security),
                new AssignRequestHandler(this, db, metadata, security),
                new AssociateRequestHandler(this, db, metadata, security),
                new DisassociateRequestHandler(this, db, metadata, security),
                new MergeRequestHandler(this, db, metadata, security),
                new RetrieveVersionRequestHandler(this, db, metadata, security),
                new FetchXmlToQueryExpressionRequestHandler(this, db, metadata, security),
                new ExecuteMultipleRequestHandler(this, db, metadata, security),
                new RetrieveEntityRequestHandler(this, db, metadata, security),
                new RetrieveRelationshipRequestHandler(this, db, metadata, security),
                new GrantAccessRequestHandler(this, db, metadata, security),
                new ModifyAccessRequestHandler(this, db, metadata, security),
                new RevokeAccessRequestHandler(this, db, metadata, security),
                new WinOpportunityRequestHandler(this, db, metadata, security),
                new LoseOpportunityRequestHandler(this, db, metadata, security),
                new RetrieveAllOptionSetsRequestHandler(this, db, metadata, security),
                new RetrieveOptionSetRequestHandler(this, db, metadata, security),
                new RetrieveExchangeRateRequestHandler(this, db, metadata, security),
                new CloseIncidentRequestHandler(this, db, metadata, security),
                new AddMembersTeamRequestHandler(this, db, metadata, security),
                new RemoveMembersTeamRequestHandler(this, db, metadata, security),
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
                new IsValidStateTransitionRequestHandler(this, db, metadata, security),
                new CalculateRollupFieldRequestHandler(this, db, metadata, security),
                new AddPrincipalToQueueRequestHandler(this, db, metadata, security),
#endif
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
                new UpsertRequestHandler(this, db, metadata, security),
#endif
        };

        internal void EnableProxyTypes(Assembly assembly) {
            foreach (var type in assembly.GetLoadableTypes()) {
                if (type.CustomAttributes
                    .FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute")
                    ?.ConstructorArguments
                    ?.FirstOrDefault()
                    .Value is string logicalName) {
                    entityTypeMap.Add(logicalName, type);
                }
            }
        }

        private OrganizationServiceProxy GetOnlineProxy() {
            if (OnlineProxy == null && settings.OnlineEnvironment.HasValue) {
                var env = settings.OnlineEnvironment.Value;
                var orgHelper = new OrganizationHelper(
                    new Uri(env.uri),
                    env.providerType,
                    env.username,
                    env.password,
                    env.domain);
                this.OnlineProxy = orgHelper.GetServiceProxy();
                if (settings.EnableProxyTypes == true)
                    OnlineProxy.EnableProxyTypes();
            }
            return OnlineProxy;
        }

        internal IOrganizationService GetWorkflowService() {
            return ServiceFactory.CreateOrganizationService(null, new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
        }

        internal bool HasType(string entityType) {
            return entityTypeMap.ContainsKey(entityType);
        }

        private Entity GetEntity(string entityType) {
            if (HasType(entityType)) {
                return (Entity)Activator.CreateInstance(entityTypeMap[entityType]);
            }
            return null;
        }

        internal DbRow GetDbRow(EntityReference entityReference) {
            return db.GetDbRow(entityReference);
        }

        internal DbRow GetDbRowOrNull(EntityReference entityReference) {
            return db.GetDbRowOrNull(entityReference);
        }

        internal DbTable GetDbTable(string tableName) {
            return db[tableName];
        }

        internal Entity GetStronglyTypedEntity(Entity entity, EntityMetadata metadata, ColumnSet colsToKeep) {
            if (HasType(entity.LogicalName)) {
                var typedEntity = GetEntity(entity.LogicalName);
                typedEntity.SetAttributes(entity.Attributes, metadata, colsToKeep);

                Utility.PopulateEntityReferenceNames(typedEntity, db);
                typedEntity.Id = entity.Id;
                typedEntity.EntityState = entity.EntityState;
                return typedEntity;
            } else {
                return entity.CloneEntity(metadata, colsToKeep);
            }
        }

        internal void AddRelatedEntities(Entity entity, RelationshipQueryCollection relatedEntityQuery, EntityReference userRef) {
            foreach (var relQuery in relatedEntityQuery) {
                var relationship = relQuery.Key;
                var queryExpr = relQuery.Value as QueryExpression;
                if (queryExpr == null) {
                    queryExpr = XmlHandling.FetchXmlToQueryExpression(((FetchExpression)relQuery.Value).Query);
                }
                var relationshipMetadata = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, queryExpr.EntityName, relationship.SchemaName);


                var oneToMany = relationshipMetadata as OneToManyRelationshipMetadata;
                var manyToMany = relationshipMetadata as ManyToManyRelationshipMetadata;

                if (oneToMany != null) {
                    if (relationship.PrimaryEntityRole == EntityRole.Referencing) {
                        var entityAttributes = db.GetEntityOrNull(entity.ToEntityReference()).Attributes;
                        if (entityAttributes.ContainsKey(oneToMany.ReferencingAttribute) && entityAttributes[oneToMany.ReferencingAttribute] != null) {
                            var referencingGuid = Utility.GetGuidFromReference(entityAttributes[oneToMany.ReferencingAttribute]);
                            queryExpr.Criteria.AddCondition(
                                new ConditionExpression(oneToMany.ReferencedAttribute, ConditionOperator.Equal, referencingGuid));
                        }
                    } else {
                        queryExpr.Criteria.AddCondition(
                            new ConditionExpression(oneToMany.ReferencingAttribute, ConditionOperator.Equal, entity.Id));
                    }
                }

                if (manyToMany != null) {
                    if (db[manyToMany.IntersectEntityName].Count() > 0) {
                        var conditions = new FilterExpression(LogicalOperator.Or);
                        if (entity.LogicalName == manyToMany.Entity1LogicalName) {
                            queryExpr.EntityName = manyToMany.Entity2LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute));

                            foreach (var id in relatedIds) {
                                conditions.AddCondition(
                                    new ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        } else {
                            queryExpr.EntityName = manyToMany.Entity1LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute));

                            foreach (var id in relatedIds) {
                                conditions.AddCondition(
                                    new ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }
                        queryExpr.Criteria = conditions;
                    }
                }
                var entities = new EntityCollection();

                if ((oneToMany != null || manyToMany != null) && queryExpr.Criteria.Conditions.Count > 0) {
                    var handler = RequestHandlers.Find(x => x is RetrieveMultipleRequestHandler);
                    var req = new RetrieveMultipleRequest {
                        Query = queryExpr
                    };
                    var resp = handler.Execute(req, userRef) as RetrieveMultipleResponse;
                    entities = resp.EntityCollection;
                }

                if (entities.Entities.Count() > 0) {
                    entity.RelatedEntities.Add(relationship, entities);
                }
            }
        }

        internal Entity GetDbEntityWithRelatedEntities(EntityReference reference, EntityRole primaryEntityRole, EntityReference userRef) {
            var entity = db.GetEntityOrNull(reference);
            if (entity == null) {
                return null;
            }

            var metadata = this.metadata.EntityMetadata.GetMetadata(entity.LogicalName);

            if (entity.RelatedEntities.Count() > 0) {
                var clone = entity.CloneEntity(metadata, new ColumnSet(true));
                db.Update(clone);
                entity = clone;
            }
            var relationQuery = new RelationshipQueryCollection();
            var relationsMetadata =
                primaryEntityRole == EntityRole.Referenced ? metadata.OneToManyRelationships : metadata.ManyToOneRelationships;
            foreach (var relationshipMeta in relationsMetadata) {
                var query = new QueryExpression(relationshipMeta.ReferencingEntity) {
                    ColumnSet = new ColumnSet(true)
                };
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName, PrimaryEntityRole = primaryEntityRole }, query);
            }

            foreach (var relationshipMeta in metadata.ManyToManyRelationships) {
                var query = new QueryExpression(relationshipMeta.IntersectEntityName) {
                    ColumnSet = new ColumnSet(true)
                };
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName }, query);
            }
            AddRelatedEntities(entity, relationQuery, userRef);
            return entity;
        }


        internal void Initialize(params Entity[] entities) {
            foreach (var entity in entities) {
                var createHandler = RequestHandlers.Find(x => x is CreateRequestHandler);
                createHandler.Execute(new CreateRequest { Target = entity }, null);
            }
        }

        /// <summary>
        /// Execute the request and trigger plugins if needed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="userRef"></param>
        /// <returns></returns>
        public OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef) {
            return Execute(request, userRef, null);
        }

        internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef, PluginContext parentPluginContext) {
            // Setup
            HandleInternalPreOperations(request, userRef);

            var primaryRef = Mappings.GetPrimaryEntityReferenceFromRequest(request);

            // Create the plugin context
            var pluginContext = new PluginContext() {
                UserId = userRef.Id,
                InitiatingUserId = userRef.Id,
                MessageName = RequestNameToMessageName(request.RequestName),
                Depth = 1,
                OrganizationName = this.OrganizationName,
                OrganizationId = this.OrganizationId,
                PrimaryEntityName = primaryRef?.LogicalName,
            };
            if (primaryRef != null) {
                var refEntity = db.GetEntityOrNull(primaryRef);
                pluginContext.PrimaryEntityId = refEntity == null ? Guid.Empty : refEntity.Id;
            }

            foreach (var prop in request.Parameters) {
                pluginContext.InputParameters[prop.Key] = prop.Value;
            }
            if (parentPluginContext != null) {
                pluginContext.ParentContext = parentPluginContext;
                pluginContext.Depth = parentPluginContext.Depth + 1;
            }
            var buRef = GetBusinessUnit(userRef);
            pluginContext.BusinessUnitId = buRef.Id;

            Mappings.RequestToEventOperation.TryGetValue(request.GetType(), out EventOperation? eventOp);

            var entityInfo = GetEntityInfo(request);

            var settings = MockupExecutionContext.GetSettings(request);
            // Validation
            if (!settings.SetUnsettableFields && (request is UpdateRequest || request is CreateRequest)) {
                var entity = request is UpdateRequest ? (request as UpdateRequest).Target : (request as CreateRequest).Target;
                Utility.RemoveUnsettableAttributes(request.RequestName, metadata.EntityMetadata.GetMetadata(entity.LogicalName), entity);
            }

            Entity preImage = null;
            Entity postImage = null;

            if (settings.TriggerProcesses && entityInfo != null) {
                preImage = TryRetrieve(primaryRef);
                if (preImage != null)
                    primaryRef.Id = preImage.Id;
            }

            if (settings.TriggerProcesses && entityInfo != null && eventOp.HasValue) {
                // Pre-validation
                pluginManager.Trigger(eventOp.Value, ExecutionStage.PreValidation, entityInfo.Item1, preImage, postImage, pluginContext, this);

                // Pre-operation
                pluginManager.Trigger(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                workflowManager.Trigger(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);

                // System pre-operation and pre-validation
                pluginManager.TriggerSystem(eventOp.Value, ExecutionStage.PreValidation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                pluginManager.TriggerSystem(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
            }

            // Core operation
            OrganizationResponse response = ExecuteRequest(request, userRef, parentPluginContext);

            // Post-operation
            if (settings.TriggerProcesses && entityInfo != null) {
                postImage = TryRetrieve(primaryRef);
                if (eventOp.HasValue) {
                    pluginManager.TriggerSystem(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                    pluginManager.Trigger(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                    workflowManager.Trigger(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                }
                workflowManager.ExecuteWaitingWorkflows(pluginContext, this);
            }

            return response;
        }

        internal void HandleInternalPreOperations(OrganizationRequest request, EntityReference userRef) {
            if (request.RequestName == "Create") {
                var entity = request["Target"] as Entity;
                if (entity.Id == Guid.Empty) {
                    entity.Id = Guid.NewGuid();
                }
                if (entity.GetAttributeValue<EntityReference>("ownerid") == null &&
                    Utility.IsSettableAttribute("ownerid", metadata.EntityMetadata.GetMetadata(entity.LogicalName))) {
                    entity["ownerid"] = userRef;
                }
            }
        }

        internal void AddTime(TimeSpan offset) {
            this.TimeOffset = this.TimeOffset.Add(offset);
            TriggerWaitingWorkflows();
        }

        private OrganizationResponse ExecuteRequest(OrganizationRequest request, EntityReference userRef, PluginContext parentPluginContext) {
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (request is AssignRequest assignRequest) {
                var targetEntity = db.GetEntityOrNull(assignRequest.Target);
                if (targetEntity.GetAttributeValue<EntityReference>("ownerid") != assignRequest.Assignee) {
                    var req = new UpdateRequest {
                        Target = new Entity(assignRequest.Target.LogicalName, assignRequest.Target.Id)
                    };
                    req.Target.Attributes["ownerid"] = assignRequest.Assignee;
                    Execute(req, userRef, parentPluginContext);
                }
                return new AssignResponse();
            }

            if (request is SetStateRequest setstateRequest) {
                var targetEntity = db.GetEntityOrNull(setstateRequest.EntityMoniker);
                if (targetEntity.GetAttributeValue<OptionSetValue>("statecode") != setstateRequest.State ||
                    targetEntity.GetAttributeValue<OptionSetValue>("statuscode") != setstateRequest.Status) {
                    var req = new UpdateRequest {
                        Target = new Entity(setstateRequest.EntityMoniker.LogicalName, setstateRequest.EntityMoniker.Id)
                    };
                    req.Target.Attributes["statecode"] = setstateRequest.State;
                    req.Target.Attributes["statuscode"] = setstateRequest.Status;
                    Execute(req, userRef, parentPluginContext);
                }
                return new SetStateResponse();
            }
#endif
            if (workflowManager.GetActionDefaultNull(request.RequestName) != null) {
                return ExecuteAction(request);
            }
            var handler = RequestHandlers.FirstOrDefault(x => x.HandlesRequest(request.RequestName));
            if (handler != null) {
                return handler.Execute(request, userRef);
            }
            
            if (settings.ExceptionFreeRequests.Contains(request.RequestName)) {
                return new OrganizationResponse();
            }

            throw new NotImplementedException("Execute for the given request has not been implemented yet.");
        }

        private string RequestNameToMessageName(string requestName) {
            switch (requestName) {
                case "LoseOpportunity": return "Lose";
                case "WinOpportunity": return "Win";
                default: return requestName;
            }
        }

        internal void TriggerWaitingWorkflows() {
            workflowManager.ExecuteWaitingWorkflows(null, this);
        }

        internal void AddWorkflow(Entity workflow) {
            workflowManager.AddWorkflow(workflow);
        }

        internal bool ContainsEntity(Entity entity) {
            var dbentity = db.GetEntityOrNull(entity.ToEntityReference());
            if (dbentity == null) return false;
            return entity.Attributes.All(a => dbentity.Attributes.ContainsKey(a.Key) && dbentity.Attributes[a.Key].Equals(a.Value));
        }

        internal void PopulateWith(Entity[] entities) {
            foreach (var entity in entities) {
                if (entity.Id == Guid.Empty) {
                    var id = Guid.NewGuid();
                    entity.Id = id;
                    entity[entity.LogicalName + "id"] = id;
                }
                db.Add(entity);
            }
        }

        internal void SetSecurityRoles(EntityReference entRef, Guid[] securityRoles) {
            security.SetSecurityRole(entRef, securityRoles);
        }

        private OrganizationResponse ExecuteAction(OrganizationRequest request) {
            var action = workflowManager.GetActionDefaultNull(request.RequestName);

            var workflow = workflowManager.ParseWorkflow(action);
            if (workflow.Input.Where(a => a.Required).Any(required => !request.Parameters.ContainsKey(required.Name))) {
                throw new FaultException($"Call to action '{request.RequestName}' but no all required input arguments were provided");
            }

            var entity = db.GetEntityOrNull(request.Parameters["Target"] as EntityReference).CloneEntity();

            var inputs = workflow.Input.Where(a => request.Parameters.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, request.Parameters[a.Name]));
            foreach (var input in inputs) {
                var argumentName = "{" + input.Key + "(Arguments)}";
                workflow.Variables.Add(argumentName, input.Value);
            }

            var postExecution = workflowManager.ExecuteWorkflow(workflow, entity, null, this);

            var outputs = workflow.Output.Where(a => postExecution.Variables.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, postExecution.Variables[a.Name]));

            var resp = new OrganizationResponse();
            foreach (var output in outputs) {
                resp.Results[output.Key] = output.Value;
            }

            postExecution.Variables = new Dictionary<string, object>();

            return resp;
        }

        #region EntityImage helpers

        private Tuple<object, string, Guid> GetEntityInfo(OrganizationRequest request) {
            Mappings.EntityImageProperty.TryGetValue(request.GetType(), out string key);
            object obj = null;
            if (key != null) {
                obj = request.Parameters[key];
            }
            if (request is WinOpportunityRequest || request is LoseOpportunityRequest) {
                var close = request is WinOpportunityRequest
                    ? (request as WinOpportunityRequest).OpportunityClose
                    : (request as LoseOpportunityRequest).OpportunityClose;
                obj = close.GetAttributeValue<EntityReference>("opportunityid");
            }

            if (obj != null) {
                var entity = obj as Entity;
                var entityRef = obj as EntityReference;

                if (entity != null) {
                    return new Tuple<object, string, Guid>(obj, entity.LogicalName, entity.Id);
                } else {
                    return new Tuple<object, string, Guid>(obj, entityRef.LogicalName, entityRef.Id);
                }
            }
            return null;
        }

        private Entity TryRetrieve(EntityReference reference) {
            return db.GetEntityOrNull(reference)?.CloneEntity();
        }

        private EntityReference GetBusinessUnit(EntityReference owner) {
            return Utility.GetBusinessUnit(db, owner);
        }
        #endregion


        internal void DisabelRegisteredPlugins(bool include)
        {
            pluginManager.DisabelRegisteredPlugins(include);
        }

        internal void RegisterAdditionalPlugins(IEnumerable<Type> basePluginTypes, PluginRegistrationScope scope)
        {
            foreach (var type in basePluginTypes)
                pluginManager.RegisterAdditionalPlugin(type, metadata.EntityMetadata, metadata.Plugins, scope);
        }

        internal void TakeSnapshot(string snapshotName)
        {
            if (snapshots.ContainsKey(snapshotName))
                snapshots.Remove(snapshotName);
            var snapshot = new Snapshot()
            {
                db = this.db.Clone(),
                AdminUserRef = this.AdminUserRef,
                OrganizationId = this.OrganizationId,
                RootBusinessUnitRef = this.RootBusinessUnitRef,
                TimeOffset = this.TimeOffset,
                security = this.security.Clone(),
            };
            snapshots.Add(snapshotName, snapshot);
        }

        internal void RestoreToSnapshot(string snapshotName)
        {
            if (!snapshots.ContainsKey(snapshotName))
                throw new KeyNotFoundException("A Snapshot with that name does not exist");
            var snapshot = snapshots[snapshotName];
            this.db = snapshot.db.Clone();
            this.security = snapshot.security.Clone();
            this.AdminUserRef = snapshot.AdminUserRef;
            this.RootBusinessUnitRef = snapshot.RootBusinessUnitRef;
            this.TimeOffset = snapshot.TimeOffset;
            this.OrganizationId = snapshot.OrganizationId;
            this.RequestHandlers = GetRequestHandlers(this.db);
        }

        internal void DeleteSnapshot(string snapshotName)
        {
            if (!snapshots.ContainsKey(snapshotName))
                throw new KeyNotFoundException("A Snapshot with that name does not exist");
            snapshots.Remove(snapshotName);
        }

        internal void ResetEnvironment() {
            this.TimeOffset = new TimeSpan();
            if (settings.IncludeAllWorkflows == false) {
                workflowManager.ResetWorkflows();
            }
            pluginManager.ResetPlugins();
            this.db = new XrmDb(metadata.EntityMetadata, GetOnlineProxy());
            this.RequestHandlers = GetRequestHandlers(db);
            InitializeDB();
            security.ResetEnvironment(db);
        }
    }

}
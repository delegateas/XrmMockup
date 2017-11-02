
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

namespace DG.Tools.XrmMockup {

    /// <summary>
    /// Class for handling all requests to the database
    /// </summary>
    internal class Core {

        private PluginManager pluginManager;
        private WorkflowManager workflowManager;
        private DataMethods dataMethods;
        private XrmMockupSettings settings;
        private MetadataSkeleton metadata;
        private Dictionary<Guid, SecurityRole> securityRoles;
        private XrmDb db;
        private Dictionary<string, Type> entityTypeMap = new Dictionary<string, Type>();
        public TimeSpan TimeOffset { get; private set; }
        public MockupServiceProviderAndFactory ServiceFactory { get; }

        public EntityReference AdminUserRef {
            get {
                return dataMethods.AdminUserRef;
            }
        }

        public EntityReference RootBusinessUnitRef {
            get {
                return dataMethods.RootBusinessUnitRef;
            }
        }
        
        public List<RequestHandler> RequestHandlers;


        /// <summary>
        /// Creates a new instance of Core
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="metadata"></param>
        /// <param name="SecurityRoles"></param>
        /// <param name="Workflows"></param>
        public Core(XrmMockupSettings Settings, MetadataSkeleton metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) {
            this.TimeOffset = new TimeSpan();
            this.db = new XrmDb(metadata.EntityMetadata);
            this.dataMethods = new DataMethods(this, db, metadata, SecurityRoles);
            this.ServiceFactory = new MockupServiceProviderAndFactory(this);
            this.settings = Settings;
            this.metadata = metadata;
            this.securityRoles = SecurityRoles.ToDictionary(x => x.RoleId, x => x);
            this.InitRequestMap();
            this.pluginManager = new PluginManager(Settings.BasePluginTypes, metadata.EntityMetadata, metadata.Plugins);
            this.workflowManager = new WorkflowManager(Settings.CodeActivityInstanceTypes, Settings.IncludeAllWorkflows, Workflows, metadata.EntityMetadata);

            this.RequestHandlers = GetRequestHandlers(db);
            this.dataMethods.InitializeSecurityRoles();
        }

        private List<RequestHandler> GetRequestHandlers(XrmDb db) {
            return new List<RequestHandler> {
                new CreateRequestHandler(this, db, metadata, dataMethods),
                new UpdateRequestHandler(this, db, metadata, dataMethods),
                new RetrieveMultipleRequestHandler(this, db, metadata, dataMethods),
                new RetrieveRequestHandler(this, db, metadata, dataMethods),
                new DeleteRequestHandler(this, db, metadata, dataMethods),
                new SetStateRequestHandler(this, db, metadata, dataMethods),
                new AssignRequestHandler(this, db, metadata, dataMethods),
                new AssociateRequestHandler(this, db, metadata, dataMethods),
                new DisassociateRequestHandler(this, db, metadata, dataMethods)
            };
        }

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

        internal Entity GetStronglyTypedEntity(Entity entity, EntityMetadata metadata, ColumnSet colsToKeep) {
            if (HasType(entity.LogicalName)) {
                var typedEntity = GetEntity(entity.LogicalName);
                typedEntity.SetAttributes(entity.Attributes, metadata, colsToKeep);

                Utility.PopulateEntityReferenceNames(typedEntity, metadata, db);
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
                    if (relationship.PrimaryEntityRole == EntityRole.Referenced) {
                        var entityAttributes = db.GetEntityOrNull(entity.ToEntityReference()).Attributes;
                        if (entityAttributes.ContainsKey(oneToMany.ReferencingAttribute) && entityAttributes[oneToMany.ReferencingAttribute] != null) {
                            var referencingGuid = Utility.GetGuidFromReference(entityAttributes[oneToMany.ReferencingAttribute]);
                            queryExpr.Criteria.AddCondition(
                                new Microsoft.Xrm.Sdk.Query.ConditionExpression(oneToMany.ReferencedAttribute, ConditionOperator.Equal, referencingGuid));
                        }
                    } else {
                        queryExpr.Criteria.AddCondition(
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression(oneToMany.ReferencingAttribute, ConditionOperator.Equal, entity.Id));
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
                                    new Microsoft.Xrm.Sdk.Query.ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        } else {
                            queryExpr.EntityName = manyToMany.Entity1LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute));

                            foreach (var id in relatedIds) {
                                conditions.AddCondition(
                                    new Microsoft.Xrm.Sdk.Query.ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }
                        queryExpr.Criteria = conditions;
                    }
                }
                var entities = new EntityCollection();

                if ((oneToMany != null || manyToMany != null) && queryExpr.Criteria.Conditions.Count > 0) {
                    var handler = RequestHandlers.Find(x => x is RetrieveMultipleRequestHandler);
                    var req = new RetrieveMultipleRequest();
                    req.Query = queryExpr;
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
                var query = new QueryExpression(relationshipMeta.ReferencingEntity);
                query.ColumnSet = new ColumnSet(true);
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName, PrimaryEntityRole = primaryEntityRole }, query);
            }

            foreach (var relationshipMeta in metadata.ManyToManyRelationships) {
                var query = new QueryExpression(relationshipMeta.IntersectEntityName);
                query.ColumnSet = new ColumnSet(true);
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName }, query);
            }
            AddRelatedEntities(entity, relationQuery, userRef);
            return entity;
        }


        internal void Initialize(params Entity[] entities) {
            foreach (var entity in entities) {
                HandleCreate(new CreateRequest { Target = entity }, null);
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
            dataMethods.HandleInternalPreOperations(request, userRef);

            var primaryRef = Mappings.GetPrimaryEntityReferenceFromRequest(request);

            // Create the plugin context
            var pluginContext = new PluginContext() {
                UserId = userRef.Id,
                InitiatingUserId = userRef.Id,
                MessageName = RequestNameToMessageName(request.RequestName),
                Depth = 1,
                OrganizationName = dataMethods.OrganizationName,
                OrganizationId = dataMethods.OrganizationId,
                PrimaryEntityName = primaryRef?.LogicalName,
            };
            if (primaryRef != null) {
                pluginContext.PrimaryEntityId = dataMethods.GetEntityId(primaryRef).GetValueOrDefault();
            }

            foreach (var prop in request.Parameters) {
                pluginContext.InputParameters[prop.Key] = prop.Value;
            }
            if (parentPluginContext != null) {
                pluginContext.ParentContext = parentPluginContext;
                pluginContext.Depth = parentPluginContext.Depth + 1;
            }
            var buRef = GetBusinessUnit(userRef);
            Console.WriteLine($"User GUID: {userRef.Id}");
            Console.WriteLine($"BU GUID: {buRef.Id}");
            pluginContext.BusinessUnitId = buRef.Id;

            Mappings.RequestToEventOperation.TryGetValue(request.GetType(), out EventOperation? eventOp);

            var entityInfo = GetEntityInfo(request);
            Entity preImage = null;
            Entity postImage = null;

            var settings = MockupExecutionContext.GetSettings(request);
            // Validation
            if (!settings.SetUnsettableFields && (request is UpdateRequest || request is CreateRequest)) {
                var entity = request is UpdateRequest ? (request as UpdateRequest).Target : (request as CreateRequest).Target;
                Utility.RemoveUnsettableAttributes(request.RequestName, metadata.EntityMetadata.GetMetadata(entity.LogicalName), entity);
            }


            // Pre operation
            if (settings.TriggerProcesses && entityInfo != null) {
                preImage = TryRetrieve(primaryRef);
                if (preImage != null) {
                    primaryRef.Id = preImage.Id;
                }
                if (eventOp.HasValue) {
                    pluginManager.Trigger(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                    workflowManager.Trigger(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                }
            }
            // Core operation
            OrganizationResponse response = ExecuteRequest(request, userRef, parentPluginContext);


            // Post operation
            if (settings.TriggerProcesses && entityInfo != null) {
                postImage = TryRetrieve(primaryRef);
                if (eventOp.HasValue) {
                    pluginManager.Trigger(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                    workflowManager.Trigger(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, this);
                }
                workflowManager.ExecuteWaitingWorkflows(pluginContext, this);
            }

            return response;
        }

        internal void AddTime(TimeSpan offset) {
            this.TimeOffset = this.TimeOffset.Add(offset);
            TriggerWaitingWorkflows();
        }

        private OrganizationResponse ExecuteRequest(OrganizationRequest request, EntityReference userRef, PluginContext parentPluginContext) {
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (request is AssignRequest assignRequest) {
                var targetEntity = dataMethods.GetEntityOrNull(assignRequest.Target);
                if (targetEntity.GetAttributeValue<EntityReference>("ownerid") != assignRequest.Assignee) {
                    var req = new UpdateRequest();
                    req.Target = new Entity(assignRequest.Target.LogicalName, assignRequest.Target.Id);
                    req.Target.Attributes["ownerid"] = assignRequest.Assignee;
                    Execute(req, userRef, parentPluginContext);
                }
                return new AssignResponse();
            }

            if (request is SetStateRequest setstateRequest) {
                var targetEntity = dataMethods.GetEntityOrNull(setstateRequest.EntityMoniker);
                if (targetEntity.GetAttributeValue<OptionSetValue>("statecode") != setstateRequest.State ||
                    targetEntity.GetAttributeValue<OptionSetValue>("statuscode") != setstateRequest.Status) {
                    var req = new UpdateRequest();
                    req.Target = new Entity(setstateRequest.EntityMoniker.LogicalName, setstateRequest.EntityMoniker.Id);
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

            // Execute matching handler function
            if (RequestHandlerMap.TryGetValue(request.RequestName, out Func<OrganizationRequest, EntityReference, OrganizationResponse> executeFunc)) {
                return executeFunc(request, userRef);
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
            return dataMethods.ContainsEntity(entity);
        }

        internal void PopulateWith(Entity[] entities) {
            dataMethods.PopulateWith(entities);
        }

        internal void SetSecurityRoles(EntityReference entRef, Guid[] securityRoles) {
            dataMethods.SetSecurityRole(entRef, securityRoles);
        }

        private OrganizationResponse ExecuteAction(OrganizationRequest request) {
            var action = workflowManager.GetActionDefaultNull(request.RequestName);

            var workflow = workflowManager.ParseWorkflow(action);
            if (workflow.Input.Where(a => a.Required).Any(required => !request.Parameters.ContainsKey(required.Name))) {
                throw new FaultException($"Call to action '{request.RequestName}' but no all required input arguments were provided");
            }

            var entity = dataMethods.GetEntityOrNull(request.Parameters["Target"] as EntityReference).CloneEntity();

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
            return dataMethods.GetEntityOrNull(reference).CloneEntity();
        }

        private EntityReference GetBusinessUnit(EntityReference owner) {
            return Utility.GetBusinessUnit(db, owner);
        }
        #endregion


        internal void ResetEnvironment() {
            this.TimeOffset = new TimeSpan();
            if (settings.IncludeAllWorkflows == false) {
                workflowManager.ResetWorkflows();
            }
            this.db = new XrmDb(metadata.EntityMetadata);
            this.RequestHandlers = GetRequestHandlers(db);
            dataMethods.ResetEnvironment(db);
        }




        #region Execute methods for the various requests

        public Dictionary<string, Func<OrganizationRequest, EntityReference, OrganizationResponse>> RequestHandlerMap = new Dictionary<string, Func<OrganizationRequest, EntityReference, OrganizationResponse>>();

        public void InitRequestMap() {
            //RequestHandlerMap.Add("Disassociate", HandleDisassociate);
            RequestHandlerMap.Add("Merge", HandleMerge);
            RequestHandlerMap.Add("RetrieveVersion", HandleRetrieveVersion);
            RequestHandlerMap.Add("FetchXmlToQueryExpression", HandleFetchXmlToQueryExpression);
            RequestHandlerMap.Add("ExecuteMultiple", HandleExecuteMultiple);
            RequestHandlerMap.Add("RetrieveEntity", HandleRetrieveEntity);
            RequestHandlerMap.Add("RetrieveRelationship", HandleRetrieveRelationship);
            RequestHandlerMap.Add("GrantAccess", HandleGrantAccess);
            RequestHandlerMap.Add("ModifyAccess", HandleModifyAccess);
            RequestHandlerMap.Add("RevokeAccess", HandleRevokeAccess);
            RequestHandlerMap.Add("WinOpportunity", HandleWinOpportunity);
            RequestHandlerMap.Add("LoseOpportunity", HandleLoseOpportunity);
            RequestHandlerMap.Add("RetrieveAllOptionSets", HandleRetrieveAllOptionSets);
            RequestHandlerMap.Add("RetrieveOptionSet", HandleRetrieveOptionSet);

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            RequestHandlerMap.Add("CalculateRollupField", HandleCalculateRollupField);
#endif
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            RequestHandlerMap.Add("Upsert", HandleUpsert);
#endif
        }




        public static T MakeRequest<T>(OrganizationRequest req) where T : OrganizationRequest {
            var typedReq = Activator.CreateInstance<T>();
            if (req.RequestName != typedReq.RequestName) {
                throw new MockupException($"Incorrect request type made. The name '{req.RequestName}' does not match '{typedReq.RequestName}'.");
            }
            typedReq.Parameters = req.Parameters;
            return typedReq;
        }

        private RetrieveOptionSetResponse HandleRetrieveOptionSet(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveOptionSetRequest>(orgRequest);
            var resp = new RetrieveOptionSetResponse();
            resp.Results["OptionSetMetadata"] = dataMethods.RetrieveOptionSet(request.Name);
            return resp;
        }

        private RetrieveAllOptionSetsResponse HandleRetrieveAllOptionSets(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveAllOptionSetsRequest>(orgRequest);
            var resp = new RetrieveAllOptionSetsResponse();
            resp.Results["OptionSetMetadata"] = dataMethods.RetrieveAllOptionSets();
            return resp;
        }

        private WinOpportunityResponse HandleWinOpportunity(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<WinOpportunityRequest>(orgRequest);
            var resp = new WinOpportunityResponse();
            dataMethods.CloseOpportunity(OpportunityState.Won, request.Status, request.OpportunityClose, userRef);
            return resp;
        }

        private LoseOpportunityResponse HandleLoseOpportunity(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<LoseOpportunityRequest>(orgRequest);
            var resp = new LoseOpportunityResponse();
            dataMethods.CloseOpportunity(OpportunityState.Lost, request.Status, request.OpportunityClose, userRef);
            return resp;
        }

        private CreateResponse HandleCreate(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CreateRequest>(orgRequest);
            var resp = new CreateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            resp.Results.Add("id", dataMethods.Create(request.Target, userRef, settings.ServiceRole));
            resp.Results["Target"] = request.Target;
            return resp;
        }

        private RetrieveVersionResponse HandleRetrieveVersion(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveVersionRequest>(orgRequest);
            var resp = new RetrieveVersionResponse();
            resp.Results["Version"] = dataMethods.RetrieveVersion();
            return resp;
        }

      

        private FetchXmlToQueryExpressionResponse HandleFetchXmlToQueryExpression(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<FetchXmlToQueryExpressionRequest>(orgRequest);
            var resp = new FetchXmlToQueryExpressionResponse();
            resp.Results["Query"] = XmlHandling.FetchXmlToQueryExpression(request.FetchXml);
            return resp;
        }
        
        private MergeResponse HandleMerge(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<MergeRequest>(orgRequest);
            var resp = new MergeResponse();
            dataMethods.Merge(request.Target, request.SubordinateId, request.UpdateContent, request.PerformParentingChecks, userRef);
            return resp;
        }

        private RetrieveRelationshipResponse HandleRetrieveRelationship(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveRelationshipRequest>(orgRequest);
            var resp = new RetrieveRelationshipResponse();
            resp.Results["RelationshipMetadata"] =
                dataMethods.GetRelationshipMetadata(request.Name, request.MetadataId, userRef);
            return resp;
        }

        private RetrieveEntityResponse HandleRetrieveEntity(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveEntityRequest>(orgRequest);
            var resp = new RetrieveEntityResponse();
            resp.Results["EntityMetadata"] = dataMethods.GetEntityMetadata(request.LogicalName, request.MetadataId, userRef);
            return resp;
        }

        private GrantAccessResponse HandleGrantAccess(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<GrantAccessRequest>(orgRequest);
            var resp = new GrantAccessResponse();
            dataMethods.GrantAccess(request.Target, request.PrincipalAccess, userRef);
            return resp;
        }

        private ModifyAccessResponse HandleModifyAccess(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<ModifyAccessRequest>(orgRequest);
            var resp = new ModifyAccessResponse();
            dataMethods.ModifyAccess(request.Target, request.PrincipalAccess, userRef);
            return resp;
        }

        private RevokeAccessResponse HandleRevokeAccess(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RevokeAccessRequest>(orgRequest);
            var resp = new RevokeAccessResponse();
            dataMethods.RevokeAccess(request.Target, request.Revokee, userRef);
            return resp;
        }

        private ExecuteMultipleResponse HandleExecuteMultiple(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<ExecuteMultipleRequest>(orgRequest);
            var toReturn = new ExecuteMultipleResponse();
            var responses = new ExecuteMultipleResponseItemCollection();
            for (var i = 0; i < request.Requests.Count; i++) {
                var resp = new ExecuteMultipleResponseItem {
                    RequestIndex = i
                };
                var r = request.Requests[i];
                try {
                    var orgResp = Execute(r, userRef);
                    if (request.Settings.ReturnResponses) {
                        resp.Response = orgResp;
                        responses.Add(resp);
                    }

                } catch (Exception e) {
                    resp.Fault = new OrganizationServiceFault {
                        Message = e.Message,
                        Timestamp = DateTime.Now
                    };
                    responses.Add(resp);
                    if (!request.Settings.ContinueOnError) {
                        toReturn.Results["Responses"] = responses;
                        return toReturn;
                    }
                }
            }
            toReturn.Results["Responses"] = responses;
            return toReturn;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        private CalculateRollupFieldResponse HandleCalculateRollupField(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CalculateRollupFieldRequest>(orgRequest);
            var resp = new CalculateRollupFieldResponse();
            resp.Results["Entity"] = dataMethods.CalculateRollUpField(request.Target, request.FieldName, userRef);
            return resp;
        }
#endif

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        private UpsertResponse HandleUpsert(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<UpsertRequest>(orgRequest);
            var resp = new UpsertResponse();
            var target = request.Target;
            var entityId = dataMethods.GetEntityId(target.ToEntityReferenceWithKeyAttributes());
            if (entityId.HasValue) {
                var req = new UpdateRequest();
                target.Id = entityId.Value;
                req.Target = target;
                Execute(req, userRef);
                resp.Results["RecordCreated"] = false;
                resp.Results["Target"] = target.ToEntityReferenceWithKeyAttributes();
            } else {
                var req = new CreateRequest();
                req.Target = target;
                target.Id = (Execute(req, userRef) as CreateResponse).id;
                resp.Results["RecordCreated"] = true;
                resp.Results["Target"] = target.ToEntityReferenceWithKeyAttributes();
            }
            return resp;
        }
#endif
        #endregion

    }

}

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
using DG.Tools.XrmMockup.Plugin;

namespace DG.Tools.XrmMockup {

    /// <summary>
    /// Class for handling all requests to the database
    /// </summary>
    internal class Core {

        private PluginManager pluginManager;
        private WorkflowManager workflowManager;
        private DataMethods dataMethods;
        private XrmMockupSettings settings;
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

        public TimeSpan TimeOffset {
            get {
                return dataMethods.TimeOffset;
            }
        }


        /// <summary>
        /// Creates a new instance of Core
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="metadata"></param>
        /// <param name="SecurityRoles"></param>
        /// <param name="Workflows"></param>
        public Core(XrmMockupSettings Settings, MetadataSkeleton metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) {
            
            this.dataMethods = new DataMethods(this, metadata, SecurityRoles);
            this.ServiceFactory = new MockupServiceProviderAndFactory(this);
            this.settings = Settings;
            this.InitRequestMap();
            this.pluginManager = new PluginManager(Settings.BasePluginTypes, metadata.EntityMetadata, metadata.Plugins);
            this.workflowManager = new WorkflowManager(Settings.CodeActivityInstanceTypes, Settings.IncludeAllWorkflows, Workflows, metadata.EntityMetadata);

            var tracingService = new TracingService();
            var factory = new MockupServiceProviderAndFactory(this, null, tracingService);
            var service = factory.CreateOrganizationService(null, new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            dataMethods.SetWorkflowServices(tracingService, factory, service);
        }

        internal void EnableProxyTypes(Assembly assembly) {
            dataMethods.EnableProxyTypes(assembly);
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

        internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef, MockupPluginContext parentPluginContext) {
            // Setup
            dataMethods.HandleInternalPreOperations(request, userRef);

            var primaryRef = Mappings.GetPrimaryEntityReferenceFromRequest(request);

            // Create the plugin context
            var pluginContext = new MockupPluginContext() {
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
                dataMethods.RemoveUnsettableAttributes(request.RequestName, entity);
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
            dataMethods.AddTime(offset);
            TriggerWaitingWorkflows();
        }

        private OrganizationResponse ExecuteRequest(OrganizationRequest request, EntityReference userRef, MockupPluginContext parentPluginContext) {
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

            // Execute matching handler function
            if (RequestHandlerMap.TryGetValue(request.RequestName, out Func<OrganizationRequest, EntityReference, OrganizationResponse> executeFunc)) {
                return executeFunc(request, userRef);
            }
            throw new NotImplementedException("Execute for the given request has not been implemented yet.");
        }

        private string RequestNameToMessageName(string requestName) {
            switch(requestName) {
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
            return dataMethods.GetBusinessUnit(owner);
        }
        #endregion


        internal void ResetEnvironment() {
            if (settings.IncludeAllWorkflows == false) {
                workflowManager.ResetWorkflows();
            }
            dataMethods.ResetEnvironment();
        }




        #region Execute methods for the various requests

        public Dictionary<string, Func<OrganizationRequest, EntityReference, OrganizationResponse>> RequestHandlerMap = new Dictionary<string, Func<OrganizationRequest, EntityReference, OrganizationResponse>>();
        
        public void InitRequestMap() {
            RequestHandlerMap.Add("RetrieveMultiple", HandleRetrieveMultiple);
            RequestHandlerMap.Add("Retrieve", HandleRetrieve);
            RequestHandlerMap.Add("Create", HandleCreate);
            RequestHandlerMap.Add("Update", HandleUpdate);
            RequestHandlerMap.Add("Delete", HandleDelete);
            RequestHandlerMap.Add("SetState", HandleSetState);
            RequestHandlerMap.Add("Assign", HandleAssign);
            RequestHandlerMap.Add("Associate", HandleAssociate);
            RequestHandlerMap.Add("Disassociate", HandleDisassociate);
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

        private RetrieveMultipleResponse HandleRetrieveMultiple(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveMultipleRequest>(orgRequest);
            var resp = new RetrieveMultipleResponse();
            resp.Results["EntityCollection"] = dataMethods.RetrieveMultiple(request.Query, userRef);
            return resp;
        }

        private RetrieveResponse HandleRetrieve(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveRequest>(orgRequest);
            var resp = new RetrieveResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            resp.Results["Entity"] = dataMethods.Retrieve(request.Target, request.ColumnSet, request.RelatedEntitiesQuery, settings.SetUnsettableFields, userRef);
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

        private UpdateResponse HandleUpdate(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<UpdateRequest>(orgRequest);
            var resp = new UpdateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            dataMethods.Update(request.Target, userRef, settings.ServiceRole);
            resp.Results["Target"] = request.Target;
            return resp;
        }

        private FetchXmlToQueryExpressionResponse HandleFetchXmlToQueryExpression(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<FetchXmlToQueryExpressionRequest>(orgRequest);
            var resp = new FetchXmlToQueryExpressionResponse();
            resp.Results["Query"] = XmlHandling.FetchXmlToQueryExpression(request.FetchXml);
            return resp;
        }

        private DeleteResponse HandleDelete(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<DeleteRequest>(orgRequest);
            var resp = new DeleteResponse();
            dataMethods.Delete(request.Target.LogicalName, request.Target.Id, userRef);
            return resp;
        }

        private SetStateResponse HandleSetState(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<SetStateRequest>(orgRequest);
            var resp = new SetStateResponse();
            dataMethods.SetState(request.EntityMoniker, request.State, request.Status, userRef);
            return resp;
        }

        private AssignResponse HandleAssign(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<AssignRequest>(orgRequest);
            var resp = new AssignResponse();
            dataMethods.Assign(request.Target, request.Assignee, userRef);
            return resp;
        }

        private AssociateResponse HandleAssociate(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<AssociateRequest>(orgRequest);
            var resp = new AssociateResponse();
            dataMethods.Associate(request.Target, request.Relationship, request.RelatedEntities, userRef);
            return resp;
        }

        private DisassociateResponse HandleDisassociate(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<DisassociateRequest>(orgRequest);
            var resp = new DisassociateResponse();
            dataMethods.Disassociate(request.Target, request.Relationship, request.RelatedEntities, userRef);
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
                var resp = new ExecuteMultipleResponseItem();
                resp.RequestIndex = i;
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

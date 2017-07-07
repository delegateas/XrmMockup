
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
using DG.Tools.Domain;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using WorkflowExecuter;
using static DG.Tools.XrmMockupBase;

namespace DG.Tools {
    /// <summary>
    /// Class for handling all requests to the database
    /// </summary>
    public class RequestHandler {

        private XrmMockupBase crm;
        private PluginManager pluginManager;
        private WorkflowManager workflowManager;
        private DataMethods dataMethods;
        private XrmMockupSettings settings;

        /// <summary>
        /// Creates a new instance of RequestHandler
        /// </summary>
        /// <param name="crm"></param>
        /// <param name="Settings"></param>
        /// <param name="metadata"></param>
        /// <param name="SecurityRoles"></param>
        /// <param name="Workflows"></param>
        public RequestHandler(XrmMockupBase crm, XrmMockupSettings Settings, MetadataSkeleton metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) {
            this.crm = crm;
            this.settings = Settings;
            this.pluginManager = new PluginManager(Settings.BasePluginTypes, metadata.Metadata, metadata.Plugins);
            this.workflowManager = new WorkflowManager(Settings.CodeActivityInstanceTypes, Settings.IncludeAllWorkflows, Workflows, metadata.Metadata);
            this.dataMethods = new DataMethods(this, metadata, SecurityRoles);
        }

        internal void EnableProxyTypes(Assembly assembly) {
            dataMethods.EnableProxyTypes(assembly);
        }

        internal void Initialize(params Entity[] entities) {
            foreach (var entity in entities) {
                Execute(new CreateRequest { Target = entity }, null);
            }
            var tracingService = new TracingService();
            var factory = new MockupServiceProviderAndFactory(crm, null, tracingService);
            var service = factory.CreateConfigurableOrganizationService(crm.AdminUser.Id, new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            dataMethods.SetWorkflowServices(tracingService, factory, service);
            dataMethods.SetAdminUser(crm.AdminUser);
        }

        internal TimeSpan GetCurrentOffset() {
            return crm.TimeOffset;
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
            if (request is CreateRequest) {
                var entity = ((CreateRequest)request).Target;
                if (entity.Id == Guid.Empty) {
                    entity.Id = Guid.NewGuid();
                }
                if (entity.Attributes.ContainsKey("ownerid") && entity["ownerid"] == null) {
                    entity["ownerid"] = userRef;
                }

            }
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            var primaryRef = GetPrimaryEntityReferenceFromRequestWithKeyAttributes(request);
#else
            var primaryRef = GetPrimaryEntityReferenceFromRequest(request);
#endif

            // Create the plugin context
            var pluginContext = new PluginContext() {
                UserId = userRef.Id,
                InitiatingUserId = userRef.Id,
                MessageName = RequestNameToMessageName(request.RequestName),
                Depth = 1,
                OrganizationName = crm.OrganizationName,
                OrganizationId = crm.OrganizationId,
                PrimaryEntityName = primaryRef?.LogicalName,
            };
            if (primaryRef != null) pluginContext.PrimaryEntityId = primaryRef.Id;

            foreach (var prop in request.Parameters) {
                pluginContext.InputParameters[prop.Key] = prop.Value;
            }
            if (parentPluginContext != null) {
                pluginContext.ParentContext = parentPluginContext;
                pluginContext.Depth = parentPluginContext.Depth + 1;
            }
            pluginContext.BusinessUnitId = GetBusinessUnit(userRef).Id;

            EventOperation? eventOp = null;
            eventDict.TryGetValue(request.GetType(), out eventOp);

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
                    pluginManager.Trigger(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, crm);
                    workflowManager.Trigger(eventOp.Value, ExecutionStage.PreOperation, entityInfo.Item1, preImage, postImage, pluginContext, crm);
                }
            }
            // Core operation
            OrganizationResponse response = ExecuteRequest(request, userRef, parentPluginContext);


            // Post operation
            if (settings.TriggerProcesses && entityInfo != null) {
                postImage = TryRetrieve(primaryRef);
                if (eventOp.HasValue) {
                    pluginManager.Trigger(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, crm);
                    workflowManager.Trigger(eventOp.Value, ExecutionStage.PostOperation, entityInfo.Item1, preImage, postImage, pluginContext, crm);
                }
                workflowManager.ExecuteWaitingWorkflows(pluginContext, crm);
            }

            return response;
        }

        private OrganizationResponse ExecuteRequest(OrganizationRequest request, EntityReference userRef, PluginContext parentPluginContext) {
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (request is AssignRequest assignRequest) {
                var targetEntity = dataMethods.GetDbEntityDefaultNull(assignRequest.Target);
                if (targetEntity.Attributes["ownerid"] as EntityReference != assignRequest.Assignee) {
                    var req = new UpdateRequest();
                    req.Target = new Entity(assignRequest.Target.LogicalName, assignRequest.Target.Id);
                    req.Target.Attributes["ownerid"] = assignRequest.Assignee;
                    Execute(req, userRef, parentPluginContext);
                }
                return new AssignResponse();
            }

            if (request is SetStateRequest setstateRequest) {
                var targetEntity = dataMethods.GetDbEntityDefaultNull(setstateRequest.EntityMoniker);
                if (targetEntity.Attributes["statecode"] as OptionSetValue != setstateRequest.State ||
                    targetEntity.Attributes["statuscode"] as OptionSetValue != setstateRequest.Status) {
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

            return ExecuteCore(request, userRef);
        }

        private string RequestNameToMessageName(string requestName) {
            switch(requestName) {
                case "LoseOpportunity": return "Lose";
                case "WinOpportunity": return "Win";
                default: return requestName;
            }
        }

        internal void TriggerWaitingWorkflows() {
            workflowManager.ExecuteWaitingWorkflows(null, crm);
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

            var entity = dataMethods.GetDbEntityDefaultNull(request.Parameters["Target"] as EntityReference).CloneEntity();

            var inputs = workflow.Input.Where(a => request.Parameters.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, request.Parameters[a.Name]));
            foreach (var input in inputs) {
                var argumentName = "{" + input.Key + "(Arguments)}";
                workflow.Variables.Add(argumentName, input.Value);
            }

            var postExecution = workflowManager.ExecuteWorkflow(workflow, entity, null, crm);

            var outputs = workflow.Output.Where(a => postExecution.Variables.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, postExecution.Variables[a.Name]));

            var resp = new OrganizationResponse();
            foreach (var output in outputs) {
                resp.Results[output.Key] = output.Value;
            }

            postExecution.Variables = new Dictionary<string, object>();

            return resp;
        }
        private EntityReference GetPrimaryEntityReferenceFromRequest(OrganizationRequest request) {
            if (request is RetrieveRequest) return ((RetrieveRequest)request).Target;
            if (request is CreateRequest) return ((CreateRequest)request).Target.ToEntityReference();
            if (request is UpdateRequest) return ((UpdateRequest)request).Target.ToEntityReference();
            if (request is DeleteRequest) return ((DeleteRequest)request).Target;
            if (request is SetStateRequest) return ((SetStateRequest)request).EntityMoniker;
            if (request is AssignRequest) return ((AssignRequest)request).Target;
            if (request is AssociateRequest) return ((AssociateRequest)request).Target;
            if (request is DisassociateRequest) return ((DisassociateRequest)request).Target;
            if (request is MergeRequest) return ((MergeRequest)request).Target;
            if (request is WinOpportunityRequest) return ((WinOpportunityRequest)request).OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
            if (request is LoseOpportunityRequest) return ((LoseOpportunityRequest)request).OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");

            return null;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        private EntityReference GetPrimaryEntityReferenceFromRequestWithKeyAttributes(OrganizationRequest request) {
            if (request is RetrieveRequest) return ((RetrieveRequest)request).Target;
            if (request is CreateRequest) return ((CreateRequest)request).Target.ToEntityReferenceWithKeyAttributes();
            if (request is UpdateRequest) return ((UpdateRequest)request).Target.ToEntityReferenceWithKeyAttributes();
            if (request is DeleteRequest) return ((DeleteRequest)request).Target;
            if (request is SetStateRequest) return ((SetStateRequest)request).EntityMoniker;
            if (request is AssignRequest) return ((AssignRequest)request).Target;
            if (request is AssociateRequest) return ((AssociateRequest)request).Target;
            if (request is DisassociateRequest) return ((DisassociateRequest)request).Target;
            if (request is MergeRequest) return ((MergeRequest)request).Target;
            if (request is WinOpportunityRequest) return ((WinOpportunityRequest)request).OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
            if (request is LoseOpportunityRequest) return ((LoseOpportunityRequest)request).OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");

            return null;
        }

#endif


        #region EntityImage helpers
        private Dictionary<Type, string> entityImageProperty = new Dictionary<Type, string>()
        {
            { typeof(AssignRequest), "Target" },
            { typeof(CreateRequest), "Target" },
            { typeof(DeleteRequest), "Target" },
            { typeof(DeliverIncomingEmailRequest), "EmailId" },
            { typeof(DeliverPromoteEmailRequest), "EmailId" },
            { typeof(ExecuteWorkflowRequest), "Target" },
            { typeof(MergeRequest), "Target" },
            { typeof(SendEmailRequest), "EmailId" },
            { typeof(SetStateRequest), "EntityMoniker" },
            { typeof(UpdateRequest), "Target" },
            { typeof(AssociateRequest), "Target" },
            { typeof(DisassociateRequest), "Target" },
        };


        private Tuple<object, string, Guid> GetEntityInfo(OrganizationRequest request) {
            string key = null;
            entityImageProperty.TryGetValue(request.GetType(), out key);
            object obj = null;
            if (key != null) {
                obj = request.Parameters[key];
            }
            if (request is WinOpportunityRequest || request is LoseOpportunityRequest) {
                var close = request is WinOpportunityRequest ?
                    (request as WinOpportunityRequest).OpportunityClose
                    :
                    (request as LoseOpportunityRequest).OpportunityClose;
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
            return dataMethods.GetDbEntityDefaultNull(reference).CloneEntity();
        }

        private EntityReference GetBusinessUnit(EntityReference owner) {
            return dataMethods.GetBusinessUnit(owner);
        }
        #endregion


        private Dictionary<Type, EventOperation?> eventDict = new Dictionary<Type, EventOperation?>()
        {
            { typeof(AssignRequest), EventOperation.Assign },
            { typeof(AssociateRequest), EventOperation.Associate },
            { typeof(CreateRequest), EventOperation.Create },
            { typeof(DeleteRequest), EventOperation.Delete },
            { typeof(DisassociateRequest), EventOperation.Disassociate },
            { typeof(GrantAccessRequest), EventOperation.GrantAccess },
            { typeof(MergeRequest), EventOperation.Merge },
            { typeof(ModifyAccessRequest), EventOperation.ModifyAccess },
            { typeof(RetrieveRequest), EventOperation.Retrieve },
            { typeof(RetrieveMultipleRequest), EventOperation.RetrieveMultiple },
            { typeof(RetrievePrincipalAccessRequest), EventOperation.RetrievePrincipalAccess },
            //{ typeof(RetrieveSharedPrincipalAccessRequest), EventOperation.RetrieveSharedPrincipalAccess }, // No such request
            { typeof(RevokeAccessRequest), EventOperation.RevokeAccess },
            { typeof(SetStateRequest), EventOperation.SetState },
            //{ typeof(SetStateDynamicEntityRequest), EventOperation.SetStateDynamicEntity }, // No such request
            { typeof(UpdateRequest), EventOperation.Update },
            { typeof(WinOpportunityRequest), EventOperation.Win },
            { typeof(LoseOpportunityRequest), EventOperation.Lose },

        };

        internal void ResetEnvironment() {
            if (settings.IncludeAllWorkflows == false) {
                workflowManager.ResetWorkflows();
            }

            dataMethods.ResetEnvironment();
        }


        #region Execute methods for the various requests
        
        private OrganizationResponse ExecuteCore(OrganizationRequest request, EntityReference userRef) {
            if (request is RetrieveMultipleRequest) return Execute((RetrieveMultipleRequest)request, userRef);
            if (request is RetrieveRequest) return Execute((RetrieveRequest)request, userRef);
            if (request is CreateRequest) return Execute((CreateRequest)request, userRef);
            if (request is UpdateRequest) return Execute((UpdateRequest)request, userRef);
            if (request is DeleteRequest) return Execute((DeleteRequest)request, userRef);
            if (request is SetStateRequest) return Execute((SetStateRequest)request, userRef);
            if (request is AssignRequest) return Execute((AssignRequest)request, userRef);
            if (request is AssociateRequest) return Execute((AssociateRequest)request, userRef);
            if (request is DisassociateRequest) return Execute((DisassociateRequest)request, userRef);
            if (request is MergeRequest) return Execute((MergeRequest)request, userRef);
            if (request is RetrieveVersionRequest) return Execute((RetrieveVersionRequest)request, userRef);
            if (request is FetchXmlToQueryExpressionRequest) return Execute((FetchXmlToQueryExpressionRequest)request, userRef);
            if (request is ExecuteMultipleRequest) return Execute((ExecuteMultipleRequest)request, userRef);
            if (request is RetrieveEntityRequest) return Execute((RetrieveEntityRequest)request, userRef);
            if (request is RetrieveRelationshipRequest) return Execute((RetrieveRelationshipRequest)request, userRef);
            if (request is GrantAccessRequest) return Execute((GrantAccessRequest)request, userRef);
            if (request is ModifyAccessRequest) return Execute((ModifyAccessRequest)request, userRef);
            if (request is RevokeAccessRequest) return Execute((RevokeAccessRequest)request, userRef);
            if (request is WinOpportunityRequest) return Execute((WinOpportunityRequest)request, userRef);
            if (request is LoseOpportunityRequest) return Execute((LoseOpportunityRequest)request, userRef);
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            if (request is CalculateRollupFieldRequest) return Execute((CalculateRollupFieldRequest)request, userRef);
#endif
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (request is UpsertRequest) return Execute((UpsertRequest)request, userRef);
#endif
            throw new NotImplementedException("Execute for the given request has not been implemented yet.");
        }

        private WinOpportunityResponse Execute(WinOpportunityRequest request, EntityReference userRef) {
            var resp = new WinOpportunityResponse();
            dataMethods.CloseOpportunity(OpportunityState.Won, request.Status, request.OpportunityClose, userRef);
            return resp;
        }

        private LoseOpportunityResponse Execute(LoseOpportunityRequest request, EntityReference userRef) {
            var resp = new LoseOpportunityResponse();
            dataMethods.CloseOpportunity(OpportunityState.Lost, request.Status, request.OpportunityClose, userRef);
            return resp;
        }

        private RetrieveMultipleResponse Execute(RetrieveMultipleRequest request, EntityReference userRef) {
            var resp = new RetrieveMultipleResponse();
            resp.Results.Add("EntityCollection", dataMethods.RetrieveMultiple(request.Query, userRef));
            return resp;
        }

        private RetrieveResponse Execute(RetrieveRequest request, EntityReference userRef) {
            var resp = new RetrieveResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            resp.Results.Add("Entity", dataMethods.Retrieve(request.Target, request.ColumnSet, request.RelatedEntitiesQuery, settings.SetUnsettableFields, userRef));
            return resp;
        }

        private CreateResponse Execute(CreateRequest request, EntityReference userRef) {
            var resp = new CreateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            resp.Results.Add("id", dataMethods.Create(request.Target, userRef, settings.ServiceRole));
            resp.Results["Target"] = request.Target;
            return resp;
        }

        private RetrieveVersionResponse Execute(RetrieveVersionRequest request, EntityReference userRef) {
            var resp = new RetrieveVersionResponse();
            resp.Results["Version"] = dataMethods.RetrieveVersion();
            return resp;
        }

        private UpdateResponse Execute(UpdateRequest request, EntityReference userRef) {
            var resp = new UpdateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            dataMethods.Update(request.Target, userRef, settings.ServiceRole);
            resp.Results["Target"] = request.Target;
            return resp;
        }

        private FetchXmlToQueryExpressionResponse Execute(FetchXmlToQueryExpressionRequest request, EntityReference userRef) {
            var resp = new FetchXmlToQueryExpressionResponse();
            resp.Results.Add("Query", XmlHandling.FetchXmlToQueryExpression(request.FetchXml));
            return resp;
        }

        private DeleteResponse Execute(DeleteRequest request, EntityReference userRef) {
            var resp = new DeleteResponse();
            dataMethods.Delete(request.Target.LogicalName, request.Target.Id, userRef);
            return resp;
        }

        private SetStateResponse Execute(SetStateRequest request, EntityReference userRef) {
            var resp = new SetStateResponse();
            dataMethods.SetState(request.EntityMoniker, request.State, request.Status, userRef);
            return resp;
        }

        private AssignResponse Execute(AssignRequest request, EntityReference userRef) {
            var resp = new AssignResponse();
            dataMethods.Assign(request.Target, request.Assignee, userRef);
            return resp;
        }

        private AssociateResponse Execute(AssociateRequest request, EntityReference userRef) {
            var resp = new AssociateResponse();
            dataMethods.Associate(request.Target, request.Relationship, request.RelatedEntities, userRef);
            return resp;
        }

        private DisassociateResponse Execute(DisassociateRequest request, EntityReference userRef) {
            var resp = new DisassociateResponse();
            dataMethods.Disassociate(request.Target, request.Relationship, request.RelatedEntities, userRef);
            return resp;
        }

        private MergeResponse Execute(MergeRequest request, EntityReference userRef) {
            var resp = new MergeResponse();
            dataMethods.Merge(request.Target, request.SubordinateId, request.UpdateContent, request.PerformParentingChecks, userRef);
            return resp;
        }

        private RetrieveRelationshipResponse Execute(RetrieveRelationshipRequest request, EntityReference userRef) {
            var resp = new RetrieveRelationshipResponse();
            resp.Results.Add("RelationshipMetadata",
                dataMethods.GetRelationshipMetadata(request.Name, request.MetadataId, userRef));
            return resp;
        }

        private RetrieveEntityResponse Execute(RetrieveEntityRequest request, EntityReference userRef) {
            var resp = new RetrieveEntityResponse();
            resp.Results.Add("EntityMetadata", dataMethods.GetEntityMetadata(request.LogicalName, request.MetadataId, userRef));
            return resp;
        }

        private GrantAccessResponse Execute(GrantAccessRequest request, EntityReference userRef) {
            var resp = new GrantAccessResponse();
            dataMethods.GrantAccess(request.Target, request.PrincipalAccess, userRef);
            return resp;
        }

        private ModifyAccessResponse Execute(ModifyAccessRequest request, EntityReference userRef) {
            var resp = new ModifyAccessResponse();
            dataMethods.ModifyAccess(request.Target, request.PrincipalAccess, userRef);
            return resp;
        }

        private RevokeAccessResponse Execute(RevokeAccessRequest request, EntityReference userRef) {
            var resp = new RevokeAccessResponse();
            dataMethods.RevokeAccess(request.Target, request.Revokee, userRef);
            return resp;
        }

        private ExecuteMultipleResponse Execute(ExecuteMultipleRequest request, EntityReference userRef) {
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
                        toReturn.Results.Add("Responses", responses);
                        return toReturn;
                    }
                }
            }
            toReturn.Results.Add("Responses", responses);
            return toReturn;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        private CalculateRollupFieldResponse Execute(CalculateRollupFieldRequest request, EntityReference userRef) {
            var resp = new CalculateRollupFieldResponse();
            resp.Results["Entity"] = dataMethods.CalculateRollUpField(request.Target, request.FieldName, userRef);
            return resp;
        }
#endif
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        private UpsertResponse Execute(UpsertRequest request, EntityReference userRef) {
            var resp = new UpsertResponse();
            var target = request.Target;
            var entityId = dataMethods.GetEntityId(target.ToEntityReference());
            if (entityId.HasValue) {
                var req = new UpdateRequest();
                target.Id = entityId.Value;
                req.Target = target;
                Execute((OrganizationRequest)req, userRef);
                resp.Results["RecordCreated"] = false;
                resp.Results["Target"] = target.ToEntityReference();
            } else {
                var req = new CreateRequest();
                req.Target = target;
                target.Id = (Execute((OrganizationRequest)req, userRef) as CreateResponse).id;
                resp.Results["RecordCreated"] = true;
                resp.Results["Target"] = target.ToEntityReference();
            }
            return resp;
        }
#endif
        #endregion

    }

}

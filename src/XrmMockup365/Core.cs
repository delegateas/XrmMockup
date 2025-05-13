using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Serialization;
using DG.XrmPluginCore.Enums;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text.Json;
using WorkflowExecuter;

[assembly: InternalsVisibleTo("SharedTests")]

namespace DG.Tools.XrmMockup
{
    internal class Snapshot
    {
        public XrmDb db;
        public Security security;
        public EntityReference AdminUserRef;
        public EntityReference RootBusinessUnitRef;
        public Guid OrganizationId;
        public TimeSpan TimeOffset;
    }


    internal class CascadeSelection
    {
        public bool assign = false;
        public bool delete = false;
        public bool merge = false;
        public bool reparent = false;
        public bool share = false;
        public bool unshare = false;
        public bool rollup = false;
    }

    /// <summary>
    /// Class for handling all requests to the database
    /// </summary>
    internal class Core : IXrmMockupExtension
    {
        #region MyRegion

        private PluginManager pluginManager;
        private CustomApiManager customApiManager;
        internal OrganizationDetail orgDetail;
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
        public ITracingServiceFactory TracingServiceFactory { get; }

        private FormulaFieldEvaluator FormulaFieldEvaluator { get; }

        private List<string> systemAttributeNames;

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
        public Core(XrmMockupSettings Settings, MetadataSkeleton metadata, List<Entity> Workflows,
            List<SecurityRole> SecurityRoles)
        {
            this.TimeOffset = new TimeSpan();
            this.settings = Settings;
            this.metadata = metadata;
            baseCurrency = metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
            baseCurrencyPrecision = metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision");

            this.db = new XrmDb(metadata.EntityMetadata, GetOnlineProxy());
            this.snapshots = new Dictionary<string, Snapshot>();
            this.security = new Security(this, metadata, SecurityRoles, db);
            this.TracingServiceFactory = settings.TracingServiceFactory ?? new TracingServiceFactory();
            this.ServiceFactory = new MockupServiceProviderAndFactory(this);

            //add the additional plugin settings to the meta data
            if (settings.IPluginMetadata != null)
            {
                metadata.Plugins.AddRange(Settings.IPluginMetadata);
            }

            this.pluginManager = new PluginManager(Settings.BasePluginTypes, metadata.EntityMetadata, metadata.Plugins);
            this.workflowManager = new WorkflowManager(Settings.CodeActivityInstanceTypes, Settings.IncludeAllWorkflows,
                Workflows, metadata.EntityMetadata);
            this.customApiManager = new CustomApiManager(Settings.BaseCustomApiTypes);

            this.systemAttributeNames = new List<string>() { "createdon", "createdby", "modifiedon", "modifiedby" };

            this.RequestHandlers = GetRequestHandlers(db);
            InitializeDB();
            this.security.InitializeSecurityRoles(db);
            this.orgDetail = settings.OrganizationDetail;

            this.FormulaFieldEvaluator = new FormulaFieldEvaluator(ServiceFactory);
        }

        private void InitializeDB()
        {
            this.OrganizationId = Guid.NewGuid();
            this.OrganizationName = "MockupOrganization";

            // Setup currencies
            var currencies = new List<Entity>();
            foreach (var entity in metadata.Currencies)
            {
                Utility.RemoveAttribute(entity, "createdby", "modifiedby", "organizationid", "modifiedonbehalfby",
                    "createdonbehalfby");
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
            var admin = new Entity(LogicalNames.SystemUser)
            {
                Id = Guid.NewGuid()
            };
            this.AdminUserRef = admin.ToEntityReference();

            admin["firstname"] = "";
            admin["lastname"] = "SYSTEM";
            admin["businessunitid"] = RootBusinessUnitRef;
            this.db.Add(admin);

            // Setup default team for root business unit
            var defaultTeam = Utility.CreateDefaultTeam(rootBu, AdminUserRef);
            this.db.Add(defaultTeam);

            // Adding admin user to root business unit default team
            var teamMembership = new Entity(LogicalNames.TeamMembership);
            teamMembership["teamid"] = defaultTeam.Id;
            teamMembership["systemuserid"] = admin.Id;
            this.db.Add(teamMembership);
        }

        private List<RequestHandler> GetRequestHandlers(XrmDb db) => new List<RequestHandler>
        {
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
            new SendEmailRequestHandler(this, db, metadata, security),
            new IsValidStateTransitionRequestHandler(this, db, metadata, security),
            new CalculateRollupFieldRequestHandler(this, db, metadata, security),
            new AddUserToRecordTeamRequestHandler(this, db, metadata, security),
            new RemoveUserFromRecordTeamRequestHandler(this, db, metadata, security),
            new RetrieveCurrentOrganizationRequestHandler(this, db, metadata, security),
            new UpsertRequestHandler(this, db, metadata, security),
            new RetrieveAttributeRequestHandler(this, db, metadata, security),
            new WhoAmIRequestHandler(this, db, metadata, security),
            new RetrieveAllEntitiesRequestHandler(this, db, metadata, security),
            new RetrievePrincipalAccessRequestHandler(this, db, metadata, security),
            new RetrieveMetadataChangesRequestHandler(this, db, metadata, security),
            new InitializeFileBlocksUploadRequestHandler(this, db, metadata, security),
            new UploadBlockRequestHandler(this, db, metadata, security),
            new CommitFileBlocksUploadRequestHandler(this, db, metadata, security),
            new InstantiateTemplateRequestHandler(this, db, metadata, security),
            new CreateMultipleRequestHandler(this, db, metadata, security),
            new UpdateMultipleRequestHandler(this, db, metadata, security),
            new UpsertMultipleRequestHandler(this, db, metadata, security),
            new ExecuteTransactionRequestHandler(this, db, metadata, security),
        };

        internal void EnableProxyTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetLoadableTypes())
            {
                if (type.CustomAttributes
                    .FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute")
                    ?.ConstructorArguments
                    ?.FirstOrDefault()
                    .Value is string logicalName)
                {
                    entityTypeMap.Add(logicalName, type);
                }
            }
        }

        private OrganizationServiceProxy GetOnlineProxy()
        {
            if (OnlineProxy == null && settings.OnlineEnvironment.HasValue)
            {
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

        internal IOrganizationService GetWorkflowService()
        {
            return ServiceFactory.CreateOrganizationService(null,
                new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
        }

        internal bool HasType(string entityType)
        {
            return entityTypeMap.ContainsKey(entityType);
        }

        private Entity GetEntity(string entityType)
        {
            if (HasType(entityType))
            {
                return (Entity)Activator.CreateInstance(entityTypeMap[entityType]);
            }

            return null;
        }

        internal DbRow GetDbRow(EntityReference entityReference)
        {
            return db.GetDbRow(entityReference);
        }

        internal DbRow GetDbRowOrNull(EntityReference entityReference)
        {
            return db.GetDbRowOrNull(entityReference);
        }

        internal DbTable GetDbTable(string tableName)
        {
            return db[tableName];
        }

        internal Entity GetStronglyTypedEntity(Entity entity, EntityMetadata metadata, ColumnSet colsToKeep)
        {
            Entity toReturn;

            if (HasType(entity.LogicalName))
            {
                toReturn = GetEntity(entity.LogicalName);
                toReturn.SetAttributes(entity.Attributes, metadata, colsToKeep);
                toReturn.RelatedEntities = entity.RelatedEntities;

                toReturn.Id = entity.Id;
                toReturn.EntityState = entity.EntityState;
            }
            else
            {
                toReturn = entity.CloneEntity(metadata, colsToKeep);
            }
            toReturn.KeyAttributes = entity.CloneKeyAttributes();
            Utility.PopulateEntityReferenceNames(toReturn, db);
            return toReturn;
        }

        internal void AddRelatedEntities(Entity entity, RelationshipQueryCollection relatedEntityQuery,
            EntityReference userRef)
        {
            foreach (var relQuery in relatedEntityQuery)
            {
                var relationship = relQuery.Key;
                if (!(relQuery.Value is QueryExpression queryExpr))
                {
                    queryExpr = XmlHandling.FetchXmlToQueryExpression(((FetchExpression)relQuery.Value).Query);
                }

                var relationshipMetadata = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata,
                    queryExpr.EntityName, relationship.SchemaName);


                var oneToMany = relationshipMetadata as OneToManyRelationshipMetadata;
                var manyToMany = relationshipMetadata as ManyToManyRelationshipMetadata;

                if (oneToMany != null)
                {
                    if (relationship.PrimaryEntityRole == EntityRole.Referencing)
                    {
                        var entityAttributes = db.GetEntityOrNull(entity.ToEntityReference()).Attributes;
                        if (entityAttributes.ContainsKey(oneToMany.ReferencingAttribute) &&
                            entityAttributes[oneToMany.ReferencingAttribute] != null)
                        {
                            var referencingGuid =
                                Utility.GetGuidFromReference(entityAttributes[oneToMany.ReferencingAttribute]);
                            queryExpr.Criteria.AddCondition(
                                new ConditionExpression(oneToMany.ReferencedAttribute, ConditionOperator.Equal,
                                    referencingGuid));
                        }
                    }
                    else
                    {
                        queryExpr.Criteria.AddCondition(
                            new ConditionExpression(oneToMany.ReferencingAttribute, ConditionOperator.Equal,
                                entity.Id));
                    }
                }

                if (manyToMany != null)
                {
                    if (db[manyToMany.IntersectEntityName].Count() > 0)
                    {
                        var conditions = new FilterExpression(LogicalOperator.Or);
                        if (entity.LogicalName == manyToMany.Entity1LogicalName)
                        {
                            queryExpr.EntityName = manyToMany.Entity2LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute));

                            foreach (var id in relatedIds)
                            {
                                conditions.AddCondition(
                                    new ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }
                        else
                        {
                            queryExpr.EntityName = manyToMany.Entity1LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute));

                            foreach (var id in relatedIds)
                            {
                                conditions.AddCondition(
                                    new ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }

                        queryExpr.Criteria = conditions;
                    }
                }

                var entities = new EntityCollection();

                if ((oneToMany != null || manyToMany != null) && queryExpr.Criteria.Conditions.Count > 0)
                {
                    var handler = RequestHandlers.Find(x => x is RetrieveMultipleRequestHandler);
                    var req = new RetrieveMultipleRequest
                    {
                        Query = queryExpr
                    };
                    var resp = handler.Execute(req, userRef) as RetrieveMultipleResponse;
                    entities = resp.EntityCollection;
                }

                if (entities.Entities.Count() > 0)
                {
                    entity.RelatedEntities.Add(relationship, entities);
                }
            }
        }

        private bool HasCascadeBehaviour(CascadeType? cascadeType)
        {
            return cascadeType != null && cascadeType != CascadeType.NoCascade;
        }

        private bool CascadeCompare(CascadeConfiguration cascadeConfiguration, CascadeSelection selection)
        {
            if (selection == null)
                return true;

            if (selection.assign && HasCascadeBehaviour(cascadeConfiguration.Assign))
                return true;

            if (selection.delete && HasCascadeBehaviour(cascadeConfiguration.Delete))
                return true;

            if (selection.merge && HasCascadeBehaviour(cascadeConfiguration.Merge))
                return true;

            if (selection.reparent && HasCascadeBehaviour(cascadeConfiguration.Reparent))
                return true;

            if (selection.share && HasCascadeBehaviour(cascadeConfiguration.Share))
                return true;

            if (selection.unshare && HasCascadeBehaviour(cascadeConfiguration.Unshare))
                return true;

            if (selection.rollup && HasCascadeBehaviour(cascadeConfiguration.RollupView))
                return true;

            return false;
        }

        //TODO: update to also take in cascading filtering on Assign, Delete, Merge, reparent, rollup
        internal Entity GetDbEntityWithRelatedEntities(EntityReference reference, EntityRole primaryEntityRole,
            EntityReference userRef, CascadeSelection cascadeSelection = null, params Relationship[] relations)
        {
            var entity = db.GetEntityOrNull(reference);
            if (entity == null)
            {
                return null;
            }

            var metadata = this.metadata.EntityMetadata.GetMetadata(entity.LogicalName);

            if (entity.RelatedEntities.Count() > 0)
            {
                var clone = entity.CloneEntity(metadata, new ColumnSet(true));
                db.Update(clone);
                entity = clone;
            }

            var relationQuery = new RelationshipQueryCollection();
            var relationsMetadata =
                primaryEntityRole == EntityRole.Referenced
                    ? metadata.OneToManyRelationships
                    : metadata.ManyToOneRelationships;

            if (cascadeSelection != null)
            {
                relationsMetadata.Where(x => CascadeCompare(x.CascadeConfiguration, cascadeSelection));
            }

            if (relations.Any())
            {
                relationsMetadata = relationsMetadata
                    .Join(relations, x => x.SchemaName, y => y.SchemaName, (r1, r2) => r1).ToArray();
            }

            relationQuery.AddRange(
                relationsMetadata
                    .Select(relationshipMeta =>
                    {
                        var rel = new Relationship()
                        {
                            SchemaName = relationshipMeta.SchemaName,
                            PrimaryEntityRole = primaryEntityRole
                        };
                        var query = new QueryExpression()
                        {
                            EntityName =
                                primaryEntityRole == EntityRole.Referenced
                                    ? relationshipMeta.ReferencingEntity
                                    : relationshipMeta.ReferencedEntity,
                            ColumnSet = new ColumnSet(true)
                        };
                        return new KeyValuePair<Relationship, QueryBase>(rel, query);
                    }));


            foreach (var relationshipMeta in relationsMetadata)
            {
            }

            if (cascadeSelection == null)
            {
                var relationShipManyMetadata = metadata.ManyToManyRelationships;
                if (relations.Any())
                {
                    relationShipManyMetadata = relationShipManyMetadata
                        .Join(relations, x => x.SchemaName, y => y.SchemaName, (r1, r2) => r1).ToArray();
                }

                relationQuery.AddRange(relationShipManyMetadata
                    .Select(relationshipMeta =>
                    {
                        var rel = new Relationship() { SchemaName = relationshipMeta.SchemaName };
                        var query = new QueryExpression(relationshipMeta.IntersectEntityName)
                        {
                            ColumnSet = new ColumnSet(true)
                        };
                        return new KeyValuePair<Relationship, QueryBase>(rel, query);
                    }));
            }

            AddRelatedEntities(entity, relationQuery, userRef);
            return entity;
        }


        internal void Initialize(params Entity[] entities)
        {
            foreach (var entity in entities)
            {
                var createHandler = RequestHandlers.Find(x => x is CreateRequestHandler);
                createHandler.Execute(new CreateRequest { Target = entity }, null);
            }
        }

        #endregion

        /// <summary>
        /// Execute the request and trigger plugins if needed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="userRef"></param>
        /// <returns></returns>
        public OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef)
        {
            return Execute(request, userRef, null);
        }

        internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef,
            PluginContext parentPluginContext)
        {
            // Setup
            HandleInternalPreOperations(request, userRef);

            var primaryRef = Mappings.GetPrimaryEntityReferenceFromRequest(request);

            // Create the plugin context
            var pluginContext = new PluginContext()
            {
                UserId = userRef.Id,
                InitiatingUserId = userRef.Id,
                MessageName = RequestNameToMessageName(request.RequestName),
                Depth = 1,
                ExtensionDepth = 1,
                OrganizationName = this.OrganizationName,
                OrganizationId = this.OrganizationId,
                PrimaryEntityName = primaryRef?.LogicalName,
            };
            if (primaryRef != null)
            {
                var refEntity = db.GetEntityOrNull(primaryRef);
                pluginContext.PrimaryEntityId = refEntity == null ? Guid.Empty : refEntity.Id;
            }

            foreach (var prop in request.Parameters)
            {
                pluginContext.InputParameters[prop.Key] = prop.Value;
            }

            if (parentPluginContext != null)
            {
                pluginContext.ParentContext = parentPluginContext;
                pluginContext.Depth = parentPluginContext.Depth + 1;
                pluginContext.ExtensionDepth = parentPluginContext.ExtensionDepth + 1;
                parentPluginContext.ExtensionDepth = pluginContext.ExtensionDepth;
            }

            var buRef = GetBusinessUnit(userRef);
            pluginContext.BusinessUnitId = buRef.Id;

            Mappings.RequestToEventOperation.TryGetValue(request.GetType(), out var eventOp);

            var entityInfo = GetEntityInfo(request);

            var settings = MockupExecutionContext.GetSettings(request);
            // Validation
            if (!settings.SetUnsettableFields && (request is UpdateRequest || request is CreateRequest))
            {
                var entity = request is UpdateRequest
                    ? (request as UpdateRequest).Target
                    : (request as CreateRequest).Target;
                Utility.RemoveUnsettableAttributes(request.RequestName,
                    metadata.EntityMetadata.GetMetadata(entity.LogicalName), entity);
            }

            var shouldTrigger = settings.TriggerProcesses && entityInfo != null;

            Entity preImage = TryRetrieve(primaryRef);
            if (preImage != null)
                primaryRef.Id = preImage.Id;

            if (shouldTrigger && eventOp is EventOperation preValidationOperation)
            {
                // System Pre-validation
                pluginManager.TriggerSystem(preValidationOperation, ExecutionStage.PreValidation, entityInfo.Item1, preImage, null, pluginContext, this);
                // Pre-validation
                pluginManager.TriggerSync(preValidationOperation, ExecutionStage.PreValidation, entityInfo.Item1, preImage, null, pluginContext, this, (_) => true);
            }

            //perform security checks for the request
            CheckRequestSecurity(request, userRef);

            //perform initialization of preoperation 
            InitializePreOperation(request, userRef, preImage);

            if (shouldTrigger && eventOp is EventOperation preOperationOperation)
            {
                // Shared variables should be moved to parent context when transitioning from 10 to 20.
                pluginContext.ParentContext = pluginContext.Clone();
                pluginContext.SharedVariables.Clear();

                // Pre-operation
                pluginManager.TriggerSync(preOperationOperation, ExecutionStage.PreOperation, entityInfo.Item1, preImage, null, pluginContext, this, (p) => p.GetExecutionOrder() == 0);
                workflowManager.TriggerSync(preOperationOperation, ExecutionStage.PreOperation, entityInfo.Item1, preImage, null, pluginContext, this);
                pluginManager.TriggerSync(preOperationOperation, ExecutionStage.PreOperation, entityInfo.Item1, preImage, null, pluginContext, this, (p) => p.GetExecutionOrder() != 0);

                // System Pre-operation
                pluginManager.TriggerSystem(preOperationOperation, ExecutionStage.PreOperation, entityInfo.Item1, preImage, null, pluginContext, this);
            }

            // Core operation
            OrganizationResponse response = ExecuteRequest(request, userRef, parentPluginContext, pluginContext);

            // Post-operation
            if (shouldTrigger)
            {
                // In RetrieveMultipleRequests, the OutputParameters bag contains the entity collection
                if (request is RetrieveMultipleRequest)
                {
                    pluginContext.OutputParameters["BusinessEntityCollection"] =
                        (response as RetrieveMultipleResponse)?.EntityCollection;
                }
                else if (request is RetrieveRequest)
                {
                    pluginContext.OutputParameters["BusinessEntity"] = TryRetrieve((request as RetrieveRequest).Target);
                }

                if (eventOp is EventOperation postOperationOperation)
                {
                    var syncPostImage = TryRetrieve(primaryRef);

                    //copy the createon etc system attributes onto the target so they are available for postoperation processing
                    if (syncPostImage != null)
                    {
                        CopySystemAttributes(syncPostImage, entityInfo.Item1 as Entity);
                    }

                    pluginManager.TriggerSystem(postOperationOperation, ExecutionStage.PostOperation, entityInfo.Item1, preImage, syncPostImage, pluginContext, this);

                    pluginManager.TriggerSync(postOperationOperation, ExecutionStage.PostOperation, entityInfo.Item1, preImage, syncPostImage, pluginContext, this, (p) => p.GetExecutionOrder() == 0);
                    workflowManager.TriggerSync(postOperationOperation, ExecutionStage.PostOperation, entityInfo.Item1, preImage, syncPostImage, pluginContext, this);
                    pluginManager.TriggerSync(postOperationOperation, ExecutionStage.PostOperation, entityInfo.Item1, preImage, syncPostImage, pluginContext, this, (p) => p.GetExecutionOrder() != 0);
                    
                    var asyncPostImage = TryRetrieve(primaryRef);
                    pluginManager.StageAsync(postOperationOperation, ExecutionStage.PostOperation, entityInfo.Item1, preImage, asyncPostImage, pluginContext, this);
                    workflowManager.StageAsync(postOperationOperation, ExecutionStage.PostOperation, entityInfo.Item1, preImage, asyncPostImage, pluginContext, this);
                }

                //When last Sync has been executed we trigger the Async jobs.
                if (parentPluginContext == null)
                {
                    pluginManager.TriggerAsyncWaitingJobs();
                    workflowManager.TriggerAsync(this);
                }

                workflowManager.ExecuteWaitingWorkflows(pluginContext, this);
            }

            // Trigger Extension
            if (this.settings.MockUpExtensions.Count != 0)
            {
                /*
                 * When moving business units, more than eight layers occur...
                 */
                if (pluginContext.ExtensionDepth > 8)
                {
                    throw new FaultException(
                        "This workflow job was canceled because the workflow that started it included an infinite loop." +
                        " Correct the workflow logic and try again.");
                }
            }

            switch (request.RequestName)
            {
                case "Create":
                    var createResponse = (CreateResponse)response;
                    var entityLogicalName = ((Entity)request.Parameters["Target"]).LogicalName;
                    var reference = primaryRef ?? new EntityReference(entityLogicalName, createResponse.id);

                    var createdEntity = GetDbRow(reference).ToEntity();
                    TriggerExtension(
                        new MockupService(this, userRef.Id, pluginContext), request,
                        createdEntity, null, userRef);
                    break;
                case "Update":
                    var updatedEntity = GetDbRow(primaryRef).ToEntity();
                    TriggerExtension(
                        new MockupService(this, userRef.Id, pluginContext), request,
                        updatedEntity, preImage, userRef);
                    break;
                case "Delete":
                    TriggerExtension(
                        new MockupService(this, userRef.Id, pluginContext), request,
                        null, preImage, userRef);
                    break;
            }

            return response;
        }

        private void CopySystemAttributes(Entity postImage, Entity item1)
        {
            if (item1 == null)
            {
                return;
            }

            foreach (var systemAttributeName in this.systemAttributeNames)
            {
                if (postImage.Contains(systemAttributeName))
                {
                    if (postImage[systemAttributeName] is EntityReference)
                    {
                        item1[systemAttributeName] = new EntityReference(
                            postImage.GetAttributeValue<EntityReference>(systemAttributeName).LogicalName,
                            postImage.GetAttributeValue<EntityReference>(systemAttributeName).Id);
                    }
                    else if (postImage[systemAttributeName] is DateTime)
                    {
                        item1[systemAttributeName] = postImage.GetAttributeValue<DateTime>(systemAttributeName);
                    }
                }
            }
        }

        internal void HandleInternalPreOperations(OrganizationRequest request, EntityReference userRef)
        {
            if (request.RequestName == "Create")
            {
                var entity = request["Target"] as Entity;
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }

                if (entity.GetAttributeValue<EntityReference>("ownerid") == null &&
                    Utility.IsValidAttribute("ownerid", metadata.EntityMetadata.GetMetadata(entity.LogicalName)))
                {
                    entity["ownerid"] = userRef;
                }
            }
        }

        internal void AddTime(TimeSpan offset)
        {
            this.TimeOffset = this.TimeOffset.Add(offset);
            TriggerWaitingWorkflows();
        }

        private OrganizationResponse ExecuteRequest(
            OrganizationRequest request, 
            EntityReference userRef,
            PluginContext parentPluginContext,
            PluginContext pluginContext)
        {
            if (request is AssignRequest assignRequest)
            {
                var targetEntity = db.GetEntityOrNull(assignRequest.Target);
                if (targetEntity.GetAttributeValue<EntityReference>("ownerid") != assignRequest.Assignee)
                {
                    var req = new UpdateRequest
                    {
                        Target = new Entity(assignRequest.Target.LogicalName, assignRequest.Target.Id)
                    };
                    req.Target.Attributes["ownerid"] = assignRequest.Assignee;
                    Execute(req, userRef, parentPluginContext);
                }

                return new AssignResponse();
            }

            if (request is SetStateRequest setstateRequest)
            {
                var targetEntity = db.GetEntityOrNull(setstateRequest.EntityMoniker);
                if (targetEntity.GetAttributeValue<OptionSetValue>("statecode") != setstateRequest.State ||
                    targetEntity.GetAttributeValue<OptionSetValue>("statuscode") != setstateRequest.Status)
                {
                    var req = new UpdateRequest
                    {
                        Target = new Entity(setstateRequest.EntityMoniker.LogicalName, setstateRequest.EntityMoniker.Id)
                    };
                    req.Target.Attributes["statecode"] = setstateRequest.State;
                    req.Target.Attributes["statuscode"] = setstateRequest.Status;
                    Execute(req, userRef, parentPluginContext);
                }

                return new SetStateResponse();
            }

            if (workflowManager.GetActionDefaultNull(request.RequestName) != null)
            {
                return ExecuteAction(request);
            }

            if (customApiManager.HandlesRequest(request.RequestName))
            {
                return customApiManager.Execute(request, this, pluginContext);
            }

            var handler = RequestHandlers.FirstOrDefault(x => x.HandlesRequest(request.RequestName));
            if (handler != null)
            {
                return handler.Execute(request, userRef);
            }

            if (settings.ExceptionFreeRequests?.Contains(request.RequestName) ?? false)
            {
                return new OrganizationResponse();
            }

            throw new NotImplementedException(
                $"Execute for the request '{request.RequestName}' has not been implemented yet.");
        }

        private void CheckRequestSecurity(OrganizationRequest request, EntityReference userRef)
        {
            var handler = RequestHandlers.FirstOrDefault(x => x.HandlesRequest(request.RequestName));
            if (handler != null)
            {
                handler.CheckSecurity(request, userRef);
            }
        }

        private void InitializePreOperation(OrganizationRequest request, EntityReference userRef, Entity preImage)
        {
            var handler = RequestHandlers.FirstOrDefault(x => x.HandlesRequest(request.RequestName));
            if(handler != null)
            {
                handler.InitializePreOperation(request, userRef, preImage);
            }
        }

        private string RequestNameToMessageName(string requestName)
        {
            switch (requestName)
            {
                case "LoseOpportunity": return "Lose";
                case "WinOpportunity": return "Win";
                default: return requestName;
            }
        }

        internal void TriggerWaitingWorkflows()
        {
            workflowManager.ExecuteWaitingWorkflows(null, this);
        }

        internal void AddWorkflow(Entity workflow)
        {
            workflowManager.AddWorkflow(workflow);
        }

        internal bool ContainsEntity(Entity entity)
        {
            var dbentity = db.GetEntityOrNull(entity.ToEntityReference());
            if (dbentity == null) return false;
            return entity.Attributes.All(a =>
                dbentity.Attributes.ContainsKey(a.Key) && dbentity.Attributes[a.Key].Equals(a.Value));
        }

        internal void PopulateWith(Entity[] entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Id == Guid.Empty)
                {
                    var id = Guid.NewGuid();
                    entity.Id = id;
                    entity[entity.LogicalName + "id"] = id;
                }

                db.Add(entity);
            }
        }

        internal Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> GetPrivilege(Guid principleId)
        {
            return security.GetPrincipalPrivilege(principleId);
        }

        internal void AddPrivileges(EntityReference entRef,
            Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> privileges)
        {
            security.AddPrinciplePrivileges(entRef.Id, privileges);
        }

        internal void SetSecurityRoles(EntityReference entRef, Guid[] securityRoles)
        {
            security.SetSecurityRole(entRef, securityRoles);
        }

        internal bool HasPermission(EntityReference entityRef, AccessRights access, EntityReference principleRef)
        {
            return security.HasPermission(entityRef, access, principleRef);
        }

        private OrganizationResponse ExecuteAction(OrganizationRequest request)
        {
            var action = workflowManager.GetActionDefaultNull(request.RequestName);

            var workflow = workflowManager.ParseWorkflow(action);
            if (workflow.Input.Where(a => a.Required).Any(required => !request.Parameters.ContainsKey(required.Name)))
            {
                throw new FaultException(
                    $"Call to action '{request.RequestName}' but no all required input arguments were provided");
            }

            var entity = db.GetEntityOrNull(request.Parameters["Target"] as EntityReference).CloneEntity();

            var inputs = workflow.Input.Where(a => request.Parameters.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, request.Parameters[a.Name]));
            foreach (var input in inputs)
            {
                var argumentName = "{" + input.Key + "(Arguments)}";
                workflow.Variables.Add(argumentName, input.Value);
            }

            var postExecution = workflowManager.ExecuteWorkflow(workflow, entity, null, this);

            var outputs = workflow.Output.Where(a => postExecution.Variables.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, postExecution.Variables[a.Name]));

            var resp = new OrganizationResponse();
            foreach (var output in outputs)
            {
                resp.Results[output.Key] = output.Value;
            }

            postExecution.Variables = new Dictionary<string, object>();

            return resp;
        }

        #region EntityImage helpers

        private Tuple<object, string, Guid> GetEntityInfo(OrganizationRequest request)
        {
            Mappings.EntityImageProperty.TryGetValue(request.GetType(), out string key);
            object obj = null;
            if (key != null)
            {
                obj = request.Parameters[key];
            }

            if (request is WinOpportunityRequest || request is LoseOpportunityRequest)
            {
                var close = request is WinOpportunityRequest
                    ? (request as WinOpportunityRequest).OpportunityClose
                    : (request as LoseOpportunityRequest).OpportunityClose;
                obj = close.GetAttributeValue<EntityReference>("opportunityid");
            }
            else if (request is CloseIncidentRequest closeIncidentRequest)
            {
                obj = closeIncidentRequest.IncidentResolution?.GetAttributeValue<EntityReference>("incidentid");
            }
            else if (request is RetrieveMultipleRequest)
            {
                var retrieve = request as RetrieveMultipleRequest;

                string entityName = null;
                switch (retrieve.Query)
                {
                    case FetchExpression fe:
                        var qe = XmlHandling.FetchXmlToQueryExpression(fe.Query);
                        entityName = qe.EntityName;
                        break;
                    case QueryExpression query:
                        entityName = query.EntityName;
                        break;
                    case QueryByAttribute qba:
                        entityName = qba.EntityName;
                        break;
                }

                if (entityName != null)
                {
                    return new Tuple<object, string, Guid>(new EntityReference
                    {
                        LogicalName = entityName,
                        Id = Guid.Empty
                    }, entityName, Guid.Empty);
                }
            }

            if (obj is Entity entity)
            {
                return new Tuple<object, string, Guid>(obj, entity.LogicalName, entity.Id);
            }

            if (obj is EntityReference entityRef)
            {
                return new Tuple<object, string, Guid>(obj, entityRef.LogicalName, entityRef.Id);
            }

            if (obj is EntityCollection entityCollection)
            {
                return new Tuple<object, string, Guid>(obj, entityCollection.EntityName, Guid.Empty);
            }

            return null;
        }


        private Entity TryRetrieve(EntityReference reference)
        {
            return db.GetEntityOrNull(reference)?.CloneEntity();
        }

        private EntityReference GetBusinessUnit(EntityReference owner)
        {
            return Utility.GetBusinessUnit(db, owner);
        }
        #endregion

        internal void DisableRegisteredPlugins(bool include)
        {
            pluginManager.DisableRegisteredPlugins(include);
        }

        internal List<string> PluginRegistrations => pluginManager.PluginRegistrations;
        internal List<string> TemporaryPluginRegistrations => pluginManager.TemporaryPluginRegistrations;
        internal List<string> SystemPluginRegistrations => pluginManager.SystemPluginRegistrations;

        internal XrmMockupSettings GetMockupSettings()
        {
            return settings;
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
        internal string TakeJsonSnapshot()
        {
            var jsonObj = new SnapshotDTO
            {
                Db = this.db.ToSerializableDTO(),
                AdminUserId = this.AdminUserRef.Id,
                OrganizationId = this.OrganizationId,
                RootBusinessUnitId = this.RootBusinessUnitRef.Id,
                TimeOffset = this.TimeOffset.Ticks,
                Security = this.security.ToSerializableDTO()
            };
            string jsonString = JsonSerializer.Serialize(jsonObj);
            return jsonString;
        }

        internal void RestoreJsonSnapshot(string json)
        {
            var jsonObj = JsonSerializer.Deserialize<SnapshotDTO>(json);
            this.db = XrmDb.RestoreSerializableDTO(this.db, jsonObj.Db);
            this.security = Security.RestoreSerializableDTO(this.security, jsonObj.Security);
            this.AdminUserRef = new EntityReference(this.AdminUserRef.LogicalName, jsonObj.AdminUserId);
            this.RootBusinessUnitRef = new EntityReference(this.RootBusinessUnitRef.LogicalName, jsonObj.RootBusinessUnitId);
            this.TimeOffset = new TimeSpan(jsonObj.TimeOffset);
            this.OrganizationId = jsonObj.OrganizationId;
            this.RequestHandlers = GetRequestHandlers(this.db);
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

        internal void ResetEnvironment()
        {
            this.TimeOffset = new TimeSpan();
            workflowManager.ResetWorkflows(settings.IncludeAllWorkflows);

            pluginManager.ResetPlugins();
            this.db = new XrmDb(metadata.EntityMetadata, GetOnlineProxy());
            this.RequestHandlers = GetRequestHandlers(db);
            InitializeDB();
            security.ResetEnvironment(db);
        }

        internal EntityMetadata GetEntityMetadata(string entityLogicalName)
        {
            return metadata.EntityMetadata[entityLogicalName];
        }

        internal void ExecuteCalculatedFields(DbRow row)
        {
            var attributes = row.Metadata.Attributes.Where(
                m => m.SourceType == (int)SourceType.CalculatedAttribute && !(m is MoneyAttributeMetadata && m.LogicalName.EndsWith("_base")));

            foreach (var attr in attributes)
            {
                var definition = Utility.GetFormulaDefinition(attr, SourceType.CalculatedAttribute);

                if (definition == null)
                {
                    var trace = ServiceFactory.GetService<ITracingService>();
                    trace.Trace($"Calculated field on {attr.EntityLogicalName} field {attr.LogicalName} is empty");
                    continue;
                }

                var tree = WorkflowConstructor.ParseCalculated(definition);
                var factory = ServiceFactory;
                tree.Execute(row.ToEntity().CloneEntity(row.Metadata, new ColumnSet(true)), TimeOffset, GetWorkflowService(),
                    factory, factory.GetService<ITracingService>());
            }
        }

        internal async System.Threading.Tasks.Task ExecuteFormulaFields(EntityMetadata entityMetadata, Entity entity)
        {
            if (!settings.EnablePowerFxFields)
                return;

            var attributes = entityMetadata.Attributes.Where(m => m.SourceType == (int)SourceType.FormulaAttribute);
            foreach (var attr in attributes)
            {
                var definition = Utility.GetFormulaDefinition(attr, SourceType.FormulaAttribute);

                if (definition == null)
                {
                    var trace = ServiceFactory.GetService<ITracingService>();
                    trace.Trace($"Formula field on {attr.EntityLogicalName} field {attr.LogicalName} is empty");
                    continue;
                }

                entity[attr.LogicalName] = await FormulaFieldEvaluator.Evaluate(definition, entity);
            }
        }

        internal void ResetTable(string tableName)
        {
            db.ResetTable(tableName);
        }

        internal SecurityRole GetSecurityRole(string roleName)
        {
            return security.GetSecurityRole(roleName);
        }

        internal void AddSecurityRole(SecurityRole role)
        {
            security.AddSecurityRole(role);
        }

        public void TriggerExtension(IOrganizationService service, OrganizationRequest request, Entity currentEntity,
            Entity preEntity, EntityReference userRef)
        {
            foreach (var mockUpExtension in settings.MockUpExtensions)
            {
                mockUpExtension.TriggerExtension(service, request, currentEntity, preEntity, userRef);
            }
        }
    }
}

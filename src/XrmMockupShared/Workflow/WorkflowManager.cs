using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using WorkflowExecuter;
using System.Activities;


namespace DG.Tools.XrmMockup {


    internal class WorkflowManager {
        private List<Entity> actions;
        private List<Entity> synchronousWorkflows;
        private List<Entity> asynchronousWorkflows;
        private List<WaitInfo> waitingWorkflows;
        private Dictionary<string, EntityMetadata> metadata;
        private Dictionary<string, CodeActivity> codeActivityTriggers;
        private Dictionary<Guid, WorkflowTree> parsedWorkflows;

        private Queue<WorkflowExecutionContext> pendingAsyncWorkflows;

        public WorkflowManager(IEnumerable<Type> codeActivityInstances, bool? IncludeAllWorkflows, List<Entity> mixedWorkflows, Dictionary<string, EntityMetadata> metadata) {
            this.metadata = metadata;
            this.actions = mixedWorkflows.Where(w => w.GetAttributeValue<OptionSetValue>("category").Value == 3).ToList();

            var workflows = mixedWorkflows.Where(w => w.GetAttributeValue<OptionSetValue>("category").Value == 0).ToList();
            synchronousWorkflows = new List<Entity>();
            asynchronousWorkflows = new List<Entity>();
            waitingWorkflows = new List<WaitInfo>();
            codeActivityTriggers = new Dictionary<string, CodeActivity>();
            parsedWorkflows = new Dictionary<Guid, WorkflowTree>();

            pendingAsyncWorkflows = new Queue<WorkflowExecutionContext>();

            if (!workflows.All(e => e.LogicalName == "workflow")) {
                throw new MockupException("Trying to parse workflow, but found a non workflow entity");
            }

            if (codeActivityInstances != null) {
                foreach (var codeActivityInstance in codeActivityInstances) {
                    foreach (var type in codeActivityInstance.Assembly.GetTypes()) {
                        if (type.BaseType != codeActivityInstance.BaseType || type.Module.Name.StartsWith("System")) continue;
                        var codeActivity = (CodeActivity)Activator.CreateInstance(type);

                        if (!codeActivityTriggers.ContainsKey(type.Name)) {
                            codeActivityTriggers.Add(type.Name, codeActivity);
                        }
                    }
                }
            }

            if (IncludeAllWorkflows != false) {
                #if XRM_MOCKUP_2011
                asynchronousWorkflows.AddRange(workflows);
                #else 
                foreach (Entity workflow in workflows) {
                    AddWorkflow(workflow);
                }
                #endif
            }
        }

        /// <summary>
        /// Trigger all plugin steps which match the given parameters.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="stage"></param>
        /// <param name="entity"></param>
        /// <param name="preImage"></param>
        /// <param name="postImage"></param>
        /// <param name="pluginContext"></param>
        /// <param name="core"></param>
        public void Trigger(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core) {
            /*Parallel.ForEach(asynchronousWorkflows, (x => ExecuteIfMatch(x, operation, stage, entity,
                    preImage, postImage, pluginContext, crm)));*/

            synchronousWorkflows.ForEach((x => ExecuteIfMatch(x, operation, stage, entity,
                    preImage, postImage, pluginContext, core)));
        }

        public void TriggerSync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {
            synchronousWorkflows.ForEach((x => ExecuteIfMatch(x, operation, stage, entity,
                    preImage, postImage, pluginContext, core)));
        }


        public void TriggerAsync(Core core)
        {
            /*// fetch changes performed by sync
            asynchronousWorkflows.ForEach(x => ExecuteIfMatch(x, operation, stage, entity,
                    preImage, postImage, pluginContext, core));*/


            //Fetch changes performed by Sync workflows and execute pending Async

            while (pendingAsyncWorkflows.Count > 0)
            {
                var workflowContext = pendingAsyncWorkflows.Dequeue();


                Entity primaryEntity = core.GetDbRow(workflowContext.primaryRef).ToEntity();

                var execution = ExecuteWorkflow(workflowContext.workflow, primaryEntity, workflowContext.pluginContext, core);

                if (execution.Variables["Wait"] != null)
                {
                    waitingWorkflows.Add(execution.Variables["Wait"] as WaitInfo);
                }
            }
        }

        public void StageAsync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {
            asynchronousWorkflows.ForEach(x => StageIfMatch(x, operation, stage, entity,
                    preImage, postImage, pluginContext, core));
        }

        internal void ExecuteWaitingWorkflows(PluginContext pluginContext, Core core) {
            var provider = new MockupServiceProviderAndFactory(core, pluginContext, new TracingService());
            var service = provider.CreateOrganizationService(null, new MockupServiceSettings(true, true, MockupServiceSettings.Role.SDK));
            foreach (var waitInfo in waitingWorkflows.ToList()) {
                waitingWorkflows.Remove(waitInfo);
                var variables = waitInfo.VariablesInstance;
                var shadowService = core.ServiceFactory.CreateAdminOrganizationService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));

                var primaryEntity = shadowService.Retrieve(waitInfo.PrimaryEntity.LogicalName, waitInfo.PrimaryEntity.Id, new ColumnSet(true));
                variables["InputEntities(\"primaryEntity\")"] = primaryEntity;
                waitInfo.Element.Execute(waitInfo.ElementIndex, ref variables, core.TimeOffset, service, provider, new TracingService());
                if (variables["Wait"] != null) {
                    waitingWorkflows.Add(variables["Wait"] as WaitInfo);
                }
            }
        }


        //Save context for Asynchronous workflows.
        public class WorkflowExecutionContext
        {
            public WorkflowTree workflow;
            public PluginContext pluginContext;
            public EntityReference primaryRef;

            public WorkflowExecutionContext(WorkflowTree workflow,PluginContext pluginContext, EntityReference primaryRef)
            {
                this.workflow = workflow;
                this.pluginContext = pluginContext;
                this.primaryRef = primaryRef;
            }
        }

        private void checkInfiniteRecursion(PluginContext pluginContext)
        {
            if (pluginContext.Depth > 8)
            {
                throw new FaultException(
                    "This workflow job was canceled because the workflow that started it included an infinite loop." +
                    " Correct the workflow logic and try again.");
            }
        }

        private PluginContext createPluginContext(PluginContext pluginContext, Entity workflow, workflow_stage? thisStage, Guid guid, string logicalName)
        {
            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Mode = ((int)workflow.GetOptionSetValue<Workflow_Mode>("mode") + 1) % 2;
            thisPluginContext.Stage = thisStage.HasValue ? (int)thisStage : (int)workflow_stage.Postoperation;
            thisPluginContext.PrimaryEntityId = guid;
            thisPluginContext.PrimaryEntityName = logicalName;

            return thisPluginContext;
        }

        private void StageIfMatch(Entity workflow, EventOperation operation, ExecutionStage stage,
            object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {
            if (workflow.LogicalName != "workflow") return;
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = entity?.Id ?? entityRef.Id;
            var logicalName = entity?.LogicalName ?? entityRef.LogicalName;

            if (workflow.GetAttributeValue<string>("primaryentity") != "" && workflow.GetAttributeValue<string>("primaryentity") != logicalName) return;

            checkInfiniteRecursion(pluginContext);

            var isCreate = operation == EventOperation.Create;
            var isUpdate = operation == EventOperation.Update;
            var isDelete = operation == EventOperation.Delete;

            if (!isCreate && !isUpdate && !isDelete) return;
            if (isCreate && (!workflow.GetAttributeValue<bool?>("triggeroncreate").HasValue || !workflow.GetAttributeValue<bool?>("triggeroncreate").Value)) return;
            if (isDelete && (!workflow.GetAttributeValue<bool?>("triggerondelete").HasValue || !workflow.GetAttributeValue<bool?>("triggerondelete").Value)) return;
            var triggerFields = new HashSet<string>();
            if (workflow.GetAttributeValue<string>("triggeronupdateattributelist") != null)
            {
                foreach (var field in workflow.GetAttributeValue<string>("triggeronupdateattributelist").Split(','))
                {
                    triggerFields.Add(field);
                }
            }
            if (isUpdate && (
                workflow.GetAttributeValue<string>("triggeronupdateattributelist") == null ||
                workflow.GetAttributeValue<string>("triggeronupdateattributelist") == "" ||
                !entity.Attributes.Any(a => workflow.GetAttributeValue<string>("triggeronupdateattributelist").Split(',').Any(f => a.Key == f)))) return;

            var thisStage = isCreate ? workflow.GetOptionSetValue<workflow_stage>("createstage") :
                (isDelete ? workflow.GetOptionSetValue<workflow_stage>("deletestage") : workflow.GetOptionSetValue<workflow_stage>("updatestage"));
            if (thisStage == null)
            {
                thisStage = workflow_stage.Postoperation;
            }

            if ((int)thisStage != (int)stage) return;

            var thisPluginContext = createPluginContext(pluginContext, workflow, thisStage, guid, logicalName);

            var parsedWorkflow = ParseWorkflow(workflow);
            if (parsedWorkflow == null) return;

            pendingAsyncWorkflows.Enqueue(new WorkflowExecutionContext(parsedWorkflow, thisPluginContext, new EntityReference(logicalName, guid)));
        }


        private void ExecuteIfMatch(Entity workflow, EventOperation operation, ExecutionStage stage,
            object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, Core core) {
            // Check if it is supposed to execute. Returns preemptively, if it should not.
            if (workflow.LogicalName != "workflow") return;
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = entity?.Id ?? entityRef.Id;
            var logicalName = entity?.LogicalName ?? entityRef.LogicalName;


            if (workflow.GetAttributeValue<string>("primaryentity") != "" && workflow.GetAttributeValue<string>("primaryentity") != logicalName) return;

            checkInfiniteRecursion(pluginContext);

            var isCreate = operation == EventOperation.Create;
            var isUpdate = operation == EventOperation.Update;
            var isDelete = operation == EventOperation.Delete;

            if (!isCreate && !isUpdate && !isDelete) return;

            if (isCreate && (!workflow.GetAttributeValue<bool?>("triggeroncreate").HasValue || !workflow.GetAttributeValue<bool?>("triggeroncreate").Value)) return;
            if (isDelete && (!workflow.GetAttributeValue<bool?>("triggerondelete").HasValue || !workflow.GetAttributeValue<bool?>("triggerondelete").Value)) return;
            var triggerFields = new HashSet<string>();
            if (workflow.GetAttributeValue<string>("triggeronupdateattributelist") != null) {
                foreach (var field in workflow.GetAttributeValue<string>("triggeronupdateattributelist").Split(',')) {
                    triggerFields.Add(field);
                }
            }
            if (isUpdate && (
                workflow.GetAttributeValue<string>("triggeronupdateattributelist") == null ||
                workflow.GetAttributeValue<string>("triggeronupdateattributelist") == "" ||
                !entity.Attributes.Any(a => workflow.GetAttributeValue<string>("triggeronupdateattributelist").Split(',').Any(f => a.Key == f)))) return;

            var thisStage = isCreate ? workflow.GetOptionSetValue<workflow_stage>("createstage") :
                (isDelete ? workflow.GetOptionSetValue<workflow_stage>("deletestage") : workflow.GetOptionSetValue<workflow_stage>("updatestage"));
            if (thisStage == null) {
                thisStage = workflow_stage.Postoperation;
            }

            if ((int)thisStage != (int)stage) return;

            var thisPluginContext = createPluginContext(pluginContext, workflow, thisStage, guid, logicalName);

            var parsedWorkflow = ParseWorkflow(workflow);
            if (parsedWorkflow == null) return;

            WorkflowTree postExecution = null; 
            if (thisStage == workflow_stage.Preoperation) {
                postExecution = ExecuteWorkflow(parsedWorkflow, preImage.CloneEntity(), thisPluginContext, core);
            } else {
                postExecution = ExecuteWorkflow(parsedWorkflow, postImage.CloneEntity(), thisPluginContext, core);
            }

            if (postExecution.Variables["Wait"] != null) {
                waitingWorkflows.Add(postExecution.Variables["Wait"] as WaitInfo);
            }
        }

        internal WorkflowTree ExecuteWorkflow(WorkflowTree workflow, Entity primaryEntity, PluginContext pluginContext, Core core) {
            var provider = new MockupServiceProviderAndFactory(core, pluginContext, new TracingService());
            var service = provider.CreateAdminOrganizationService(new MockupServiceSettings(true, true, MockupServiceSettings.Role.SDK));
            return workflow.Execute(primaryEntity, core.TimeOffset, service, provider, provider.GetService(typeof(ITracingService)) as ITracingService);
        }

        internal WorkflowTree ParseWorkflow(Entity workflow) {
            if (parsedWorkflows.ContainsKey(workflow.Id)) {
                parsedWorkflows[workflow.Id].HardReset();
                return parsedWorkflows[workflow.Id];
            }
            try {
                var parsed = WorkflowConstructor.Parse(workflow, codeActivityTriggers, WorkflowConstructor.ParseAs.Workflow);
                parsedWorkflows.Add(workflow.Id, parsed);
                return parsed;
            } catch (Exception) {
                Console.WriteLine($"Tried to parse workflow with name '{workflow.Attributes["name"]}' but failed");
            }
            return null;
        }

        internal void AddWorkflow(Entity workflow) {
            if (workflow.LogicalName != LogicalNames.Workflow) return;
#if XRM_MOCKUP_2011
               asynchronousWorkflows.Add(workflow);
#else
            if (workflow.GetOptionSetValue<Workflow_Mode>("mode") == Workflow_Mode.Background) {
                asynchronousWorkflows.Add(workflow);
            } else {
                synchronousWorkflows.Add(workflow);
            }
#endif
        }

        public Entity GetActionDefaultNull(string requestName) {
            return actions.FirstOrDefault(e => e.GetAttributeValue<string>("name") == requestName);
        }

        internal void ResetWorkflows() {
            synchronousWorkflows = new List<Entity>();
            asynchronousWorkflows = new List<Entity>();
            parsedWorkflows = new Dictionary<Guid, WorkflowTree>();
            waitingWorkflows = new List<WaitInfo>();
        }
    }
}

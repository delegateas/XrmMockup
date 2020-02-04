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
using System.Runtime.ExceptionServices;
using XrmMockupShared.Plugin;

namespace DG.Tools.XrmMockup {

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

    internal class PluginManager {

        private Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredPlugins;
        private Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> temporaryPlugins;
        private Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredSystemPlugins;

        // Queue for AsyncPlugins
        private Queue<PluginExecutionProvider> pendingAsyncPlugins = new Queue<PluginExecutionProvider>();

        private bool disableRegisteredPlugins = false;
        
        // List of SystemPlugins to execute
        private List<MockupPlugin> systemPlugins = new List<MockupPlugin>
        {
            //new SystemPlugins.ContactDefaultValues()
            new SystemPlugins.UpdateInactiveIncident(),
            new SystemPlugins.DefaultBusinessUnitTeams()
        };

        public PluginManager(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins)
        {
            registeredPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            temporaryPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            registeredSystemPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();

            RegisterPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
            RegisterSystemPlugins(registeredSystemPlugins, metadata);
        }

        private void RegisterPlugins(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            foreach (var basePluginType in basePluginTypes)
            {
                if (basePluginType == null) continue;
                Assembly proxyTypeAssembly = basePluginType.Assembly;

                foreach (var type in proxyTypeAssembly.GetLoadableTypes())
                {
                    if (type.BaseType != null && (type.BaseType == basePluginType || (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == basePluginType))) { 
                        RegisterPlugin(type, metadata, plugins, register);
                    }
                }
            }
            SortAllLists(register);
        }


        private void RegisterPlugin(Type basePluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            var plugin = Activator.CreateInstance(basePluginType);

            Action<MockupServiceProviderAndFactory> pluginExecute = null;
            var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>();

            if (basePluginType.GetMethod("PluginProcessingStepConfigs") != null)
            { // Matches DAXIF plugin registration
                stepConfigs.AddRange(
                    basePluginType
                    .GetMethod("PluginProcessingStepConfigs")
                    .Invoke(plugin, new object[] { })
                    as IEnumerable<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>);
                pluginExecute = (provider) => {
                    basePluginType
                    .GetMethod("Execute")
                    .Invoke(plugin, new object[] { provider });
                };
            }
            else
            { // Retrieve registration from CRM metadata
                var metaSteps = plugins.Where(x => x.AssemblyName == basePluginType.FullName).ToList();
                if (metaSteps == null || metaSteps.Count == 0)
                {
                    throw new MockupException($"Unknown plugin '{basePluginType.FullName}', please use DAXIF registration or make sure the plugin is uploaded to CRM.");
                }

                foreach(var metaStep in metaSteps) { 
                    var stepConfig = new StepConfig(metaStep.AssemblyName, metaStep.Stage, metaStep.MessageName, metaStep.PrimaryEntity);
                    var extendedConfig = new ExtendedStepConfig(0, metaStep.Mode, metaStep.Name, metaStep.Rank, metaStep.FilteredAttributes, Guid.Empty.ToString());
                    var imageTuple = metaStep.Images?.Select(x => new ImageTuple(x.Name, x.EntityAlias, x.ImageType, x.Attributes)).ToList() ?? new List<ImageTuple>();
                    stepConfigs.Add(new Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>(stepConfig, extendedConfig, imageTuple));
                    pluginExecute = (provider) => {
                        basePluginType
                        .GetMethod("Execute")
                        .Invoke(plugin, new object[] { provider });
                    };
                }
            }

            // Add discovered plugin triggers
            foreach (var stepConfig in stepConfigs)
            {
                var operation = (EventOperation)Enum.Parse(typeof(EventOperation), stepConfig.Item1.Item3);
                var stage = (ExecutionStage)stepConfig.Item1.Item2;
                var trigger = new PluginTrigger(operation, stage, pluginExecute, stepConfig, metadata);

                AddTrigger(operation, stage, trigger, register);
            }
        }

        public void ResetPlugins()
        {
            disableRegisteredPlugins = false;
            temporaryPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
        }

        public void DisabelRegisteredPlugins(bool disable)
        {
            disableRegisteredPlugins = disable;
        }

        public void RegisterAdditionalPlugin(Type pluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, PluginRegistrationScope scope)
        {
            if (pluginType.GetMethod("PluginProcessingStepConfigs") == null)
                throw new MockupException($"Unknown plugin '{pluginType.FullName}', please use the MockPlugin to register your plugin.");
            if (scope == PluginRegistrationScope.Permanent)
            {
                RegisterPlugin(pluginType, metadata, plugins, registeredPlugins);
            }
            else if (scope == PluginRegistrationScope.Temporary)
            {
                RegisterPlugin(pluginType, metadata, plugins, temporaryPlugins);
            }
        }

        private void RegisterSystemPlugins(Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register, Dictionary<string, EntityMetadata> metadata)
        {
            Action<MockupServiceProviderAndFactory> pluginExecute = null;
            var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>();

            foreach (var plugin in systemPlugins)
            {
                stepConfigs.AddRange(plugin.PluginProcessingStepConfigs());
                pluginExecute = (provider) => plugin.Execute(provider);

                // Add discovered plugin triggers
                foreach (var stepConfig in stepConfigs)
                {
                    var operation = (EventOperation)Enum.Parse(typeof(EventOperation), stepConfig.Item1.Item3);
                    var stage = (ExecutionStage)stepConfig.Item1.Item2;
                    var trigger = new PluginTrigger(operation, stage, pluginExecute, stepConfig, metadata);

                    AddTrigger(operation, stage, trigger, register);
                }
            }
            SortAllLists(register);
        }

        public void AddTrigger(EventOperation operation, ExecutionStage stage, PluginTrigger trigger, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register) {
            if (!register.ContainsKey(operation)) {
                register.Add(operation, new Dictionary<ExecutionStage, List<PluginTrigger>>());
            }
            if (!register[operation].ContainsKey(stage)) {
                register[operation].Add(stage, new List<PluginTrigger>());
            }
            register[operation][stage].Add(trigger);
        }

        /// <summary>
        /// Sorts all the registered which shares the same entry point based on their given order
        /// </summary>
        private void SortAllLists(Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> plugins)
        {
            foreach (var dictEntry in plugins)
            {
                foreach (var listEntry in dictEntry.Value)
                {
                    listEntry.Value.Sort();
                }
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
            
            if (!disableRegisteredPlugins && registeredPlugins.ContainsKey(operation) && registeredPlugins[operation].ContainsKey(stage))
                registeredPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
            if(temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
                temporaryPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }


        //Post operation - Trigger Sync and Async in that order
        public void TriggerSync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {

            if (!disableRegisteredPlugins && registeredPlugins.ContainsKey(operation) && registeredPlugins[operation].ContainsKey(stage))
                registeredPlugins[operation][stage].Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .OrderBy(p => p.GetExecutionOrder()).ToList().ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
            if (temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
                temporaryPlugins[operation][stage].Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .OrderBy(p => p.GetExecutionOrder()).ToList().ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }

        public void StageAsync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core) 
        {
            if (!disableRegisteredPlugins && registeredPlugins.ContainsKey(operation) && registeredPlugins[operation].ContainsKey(stage))
            {   
                var asyncExecutors = registeredPlugins[operation][stage].Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder()).ToList().Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, core));
                asyncExecutors.ToList().ForEach(x => pendingAsyncPlugins.Enqueue(x));
            }
            if (temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
            {
                var asyncExecutors = temporaryPlugins[operation][stage].Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder()).ToList().Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, core));
                asyncExecutors.ToList().ForEach(x => pendingAsyncPlugins.Enqueue(x));
            }
        }

        public void TriggerAsyncWaitingJobs()
        {
           while (pendingAsyncPlugins.Count > 0)
            {
                var pendingPlugin = pendingAsyncPlugins.Dequeue();

                if (pendingPlugin != null)
                {
                    pendingPlugin.ExecuteAction();
                }                
            }
        }

        public void TriggerSystem(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {
            if (!this.registeredSystemPlugins.ContainsKey(operation)) return;
            if (!this.registeredSystemPlugins[operation].ContainsKey(stage)) return;

            registeredSystemPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }

        internal class PluginTrigger : IComparable<PluginTrigger> {
            public Action<MockupServiceProviderAndFactory> pluginExecute;

            string entityName;
            EventOperation operation;
            ExecutionStage stage;
            ExecutionMode mode;
            int order = 0;
            Dictionary<string, EntityMetadata> metadata;

            HashSet<string> attributes;
            IEnumerable<ImageTuple> images;

            public PluginTrigger(EventOperation operation, ExecutionStage stage,
                    Action<MockupServiceProviderAndFactory> pluginExecute, Tuple<StepConfig, ExtendedStepConfig,
                        IEnumerable<ImageTuple>> stepConfig, Dictionary<string, EntityMetadata> metadata) {
                this.pluginExecute = pluginExecute;
                this.entityName = stepConfig.Item1.Item4;
                this.operation = operation;
                this.stage = stage;
                this.mode = (ExecutionMode)stepConfig.Item2.Item2;
                this.order = stepConfig.Item2.Item4;
                this.images = stepConfig.Item3;
                this.metadata = metadata;

                var attrs = stepConfig.Item2.Item5 ?? "";
                this.attributes = String.IsNullOrWhiteSpace(attrs) ? new HashSet<string>() : new HashSet<string>(attrs.Split(','));
            }

            public ExecutionMode GetExecutionMode()
            {
                return mode;
            }

            public int GetExecutionOrder()
            {
                return order;
            }

            // Saves "execution" for Async plugins to be executed after sync plugins.
            public PluginExecutionProvider ToPluginExecution(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
            {
                var entity = entityObject as Entity;
                var entityRef = entityObject as EntityReference;

                var guid = (entity != null) ? entity.Id : entityRef.Id;
                var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;

                if (VerifyPluginTrigger(entity,logicalName,guid,preImage,postImage,pluginContext))
                {
                    // Create the plugin context
                    var thisPluginContext = CreatePluginContext(pluginContext, guid, logicalName, preImage, postImage);
                    return new PluginExecutionProvider(pluginExecute, new MockupServiceProviderAndFactory(core, thisPluginContext, new TracingService()));
                }

                return null;             
            }

            public void ExecuteIfMatch(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, Core core) {
                // Check if it is supposed to execute. Returns preemptively, if it should not.
                var entity = entityObject as Entity;
                var entityRef = entityObject as EntityReference;

                var guid = (entity != null) ? entity.Id : entityRef.Id;
                var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;

                if (VerifyPluginTrigger(entity, logicalName, guid, preImage, postImage, pluginContext))
                {
                    var thisPluginContext = CreatePluginContext(pluginContext, guid, logicalName, preImage, postImage);

                    //Create Serviceprovider, and execute plugin
                    MockupServiceProviderAndFactory provider = new MockupServiceProviderAndFactory(core, thisPluginContext, new TracingService());
                    try
                    {
                        pluginExecute(provider);
                    }
                    catch (TargetInvocationException e)
                    {
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }

                    foreach (var parameter in thisPluginContext.SharedVariables)
                    {
                        pluginContext.SharedVariables[parameter.Key] = parameter.Value;
                    }
                }
            } 
            
            private void CheckInfiniteLoop(PluginContext pluginContext)
            {
                if (pluginContext.Depth > 8)
                {
                    throw new FaultException(
                        "This workflow job was canceled because the workflow that started it included an infinite loop." +
                        " Correct the workflow logic and try again.");
                }
            }

            private void CheckSpecialRequest()
            {
                if (entityName != "" && (operation == EventOperation.Associate || operation == EventOperation.Disassociate))
                {
                    throw new MockupException(
                        $"An {operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
                }
            }

            private Entity AddPostImageAttributesToEntity(Entity entity, Entity preImage, Entity postImage)
            {
                if (operation == EventOperation.Update && stage == ExecutionStage.PostOperation)
                {
                    var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                    entity = entity.CloneEntity();
                    entity.Attributes.AddRange(shadowAddedAttributes);
                }
                return entity;
            }

            private bool FilteredAttributesMatches(Entity entity)
            {
                if (operation != EventOperation.Update || attributes.Count == 0)
                {
                    return true;
                }
                
                bool foundAttr = false;
                foreach (var attr in entity.Attributes)
                {
                    if (attributes.Contains(attr.Key))
                    {
                        foundAttr = true;
                        break;
                    }
                }
                return foundAttr;
            }

            private bool VerifyPluginTrigger(Entity entity, string logicalName, Guid guid, Entity preImage, Entity postImage, PluginContext pluginContext)
            {
                if (entityName != "" && entityName != logicalName) return false;

                if (entity != null && metadata.GetMetadata(logicalName)?.PrimaryIdAttribute != null)
                {
                    entity[metadata.GetMetadata(logicalName).PrimaryIdAttribute] = guid;
                }

                CheckInfiniteLoop(pluginContext);
                entity = AddPostImageAttributesToEntity(entity,preImage,postImage);
                CheckSpecialRequest();

                if (FilteredAttributesMatches(entity))
                {
                    return true;
                }
                return false;
            }

            private PluginContext CreatePluginContext(PluginContext pluginContext, Guid guid, string logicalName, Entity preImage,Entity postImage)
            {
                var thisPluginContext = pluginContext.Clone();
                thisPluginContext.Mode = (int) this.mode;
                thisPluginContext.Stage = (int) this.stage;
                if (thisPluginContext.PrimaryEntityId == Guid.Empty)
                {
                    thisPluginContext.PrimaryEntityId = guid;
                }
                thisPluginContext.PrimaryEntityName = logicalName;

                foreach (var image in this.images)
                {
                    var type = (ImageType)image.Item3;
                    var cols = image.Item4 != null ? new ColumnSet(image.Item4.Split(',')) : new ColumnSet(true);
                    if (postImage != null && stage == ExecutionStage.PostOperation && (type == ImageType.PostImage || type == ImageType.Both))
                    {
                        thisPluginContext.PostEntityImages.Add(image.Item1, postImage.CloneEntity(metadata.GetMetadata(postImage.LogicalName), cols));
                    }
                    if (preImage != null && type == ImageType.PreImage || type == ImageType.Both)
                    {
                        thisPluginContext.PreEntityImages.Add(image.Item1, preImage.CloneEntity(metadata.GetMetadata(preImage.LogicalName), cols));
                    }
                }
                return thisPluginContext;
            }

            public int CompareTo(PluginTrigger other) {
                return this.order.CompareTo(other.order);
            }
        }
    }
}
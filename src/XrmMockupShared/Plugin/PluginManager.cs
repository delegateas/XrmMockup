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
using System.Diagnostics;

namespace DG.Tools.XrmMockup {

    using StepConfigTuple = Tuple<string, int, string, string>;
    using ExtendedStepConfigTuple = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;
    
    [DebuggerDisplay("{Name} : {EntityAlias} : {ImageType} : {Attributes}")]
    public class Image
    {
        public Image(string name, string entityAlias, int imageType, string attributes)
        {
            Name = name;
            EntityAlias = entityAlias;
            ImageType = imageType;
            Attributes = attributes;
        }

        public Image(ImageTuple imageTuple)
        {
            Name = imageTuple.Item1;
            EntityAlias = imageTuple.Item2;
            ImageType = imageTuple.Item3;
            Attributes = imageTuple.Item4;
        }

        public string Name { get; set; }
        public int ImageType { get; set; }
        public string EntityAlias { get; set; }
        public string Attributes { get; set; }
    }

    [DebuggerDisplay("{Deployment} : {ExecutionMode} : {Name} : {ImpersonatingUserId}")]
    public class ExtendedStepConfig
    {
        public ExtendedStepConfig()
        { }


        public ExtendedStepConfig(int deployment, int executionMode, string name, int executionOrder, string filteredAttributes,Guid? impersonatingUserId = null)
        {
            Deployment = deployment;
            ExecutionMode = executionMode;
            Name = name;
            ExecutionOrder = executionOrder;
            FilteredAttributes = filteredAttributes;
            ImpersonatingUserId = impersonatingUserId;
        }

        public ExtendedStepConfig(ExtendedStepConfigTuple extendedStepConfigTuple)
        {
            Deployment = extendedStepConfigTuple.Item1;
            ExecutionMode = extendedStepConfigTuple.Item2;
            Name = extendedStepConfigTuple.Item3;
            ExecutionOrder = extendedStepConfigTuple.Item4;
            FilteredAttributes = extendedStepConfigTuple.Item5;
            ImpersonatingUserId = string.IsNullOrEmpty(extendedStepConfigTuple.Item6) ? (Guid?)null : Guid.Parse(extendedStepConfigTuple.Item6) ;
        }

        public int Deployment { get; set; }
        public int ExecutionMode { get; set; }
        public string Name { get; set; }
        public int ExecutionOrder{ get; set; }
        public string FilteredAttributes { get; set; }
        public Guid? ImpersonatingUserId { get; set; }
    }

    [DebuggerDisplay ("{ClassName} : {ExecutionStage} : {EventOperation}") ]
    public class StepConfig
    {

        public StepConfig()
        { }

        public StepConfig(string className, int executionStage, string eventOperation, string logicalName)
        {
            ClassName = className;
            ExecutionStage = executionStage;
            EventOperation = eventOperation;
            LogicalName = logicalName;
        }

        public StepConfig(StepConfigTuple stepConfigTuple)
        {
            ClassName = stepConfigTuple.Item1;
            ExecutionStage = stepConfigTuple.Item2;
            EventOperation = stepConfigTuple.Item3;
            LogicalName = stepConfigTuple.Item4;
        }

        public string ClassName { get; set; }
        public int ExecutionStage { get; set; }
        public string EventOperation { get; set; }
        public string LogicalName { get; set; }
    }

    internal class PluginManager {

        private Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredPlugins;
        private Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> temporaryPlugins;
        private Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredSystemPlugins;

        // Queue for AsyncPlugins
        private Queue<PluginExecutionProvider> pendingAsyncPlugins = new Queue<PluginExecutionProvider>();

        private bool disableRegisteredPlugins = false;
        
        // List of SystemPlugins to execute
        private List<MockupPlugin> systemPlugins = new List<MockupPlugin>
        {
            new SystemPlugins.UpdateInactiveIncident(),
            new SystemPlugins.DefaultBusinessUnitTeams(),
            new SystemPlugins.DefaultBusinessUnitTeamMembers()
        };

        public PluginManager(IEnumerable<Type> basePluginTypes, IEnumerable<Type> pluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins)
        {
            registeredPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            temporaryPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            registeredSystemPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();

            RegisterPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
            RegisterDirectPlugins(pluginTypes, metadata, plugins, registeredPlugins);
            RegisterSystemPlugins(registeredSystemPlugins, metadata);
        }

        private void RegisterPlugins(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
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

        private void RegisterDirectPlugins(IEnumerable<Type> pluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            if (pluginTypes == null) return;

            foreach (var pluginType in pluginTypes)
            {
                if (pluginType == null) continue;
                Assembly proxyTypeAssembly = pluginType.Assembly;

                foreach (var type in proxyTypeAssembly.GetLoadableTypes())
                {
                    if (type.GetInterface("IPlugin") == typeof(IPlugin) && type.BaseType == typeof(Object))
                    {
                        RegisterPlugin(type, metadata, plugins, register);
                    }

                }
            }
            SortAllLists(register);
        }

        private void RegisterPlugin(Type basePluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            if (basePluginType.IsAbstract) return;

            var plugin = Activator.CreateInstance(basePluginType);
            
            Action<MockupServiceProviderAndFactory> pluginExecute = null;
            var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<Image>>>();

            if (basePluginType.GetMethod("PluginProcessingStepConfigs") != null)
            { // Matches DAXIF plugin registration

                var tuple = basePluginType
                    .GetMethod("PluginProcessingStepConfigs")
                    .Invoke(plugin, new object[] { })
                    as IEnumerable<Tuple<StepConfigTuple, ExtendedStepConfigTuple, IEnumerable<ImageTuple>>>;

                var config = tuple.Select(x => new Tuple<StepConfig,ExtendedStepConfig,IEnumerable<Image>>(new StepConfig(x.Item1),new ExtendedStepConfig(x.Item2),x.Item3.Select(y=> new Image(y))));

                stepConfigs.AddRange(config);
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
                    var extendedConfig = new ExtendedStepConfig(0, metaStep.Mode, metaStep.Name, metaStep.Rank, metaStep.FilteredAttributes,metaStep.ImpersonatingUserId);
                    var imageTuple = metaStep.Images?.Select(x => new Image(x.Name, x.EntityAlias, x.ImageType, x.Attributes)).ToList() ?? new List<Image>();
                    stepConfigs.Add(new Tuple<StepConfig, ExtendedStepConfig, IEnumerable<Image>>(stepConfig, extendedConfig, imageTuple));
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
                var operation = stepConfig.Item1.EventOperation;
                var stage = (ExecutionStage)stepConfig.Item1.ExecutionStage;
                var impersonatingUserId = stepConfig.Item2.ImpersonatingUserId;
                var trigger = new PluginTrigger(operation, stage, pluginExecute, stepConfig, metadata,impersonatingUserId);

                AddTrigger(operation, stage, trigger, register);
            }
        }

        public void ResetPlugins()
        {
            disableRegisteredPlugins = false;
            temporaryPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
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

        private void RegisterSystemPlugins(Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register, Dictionary<string, EntityMetadata> metadata)
        {
            Action<MockupServiceProviderAndFactory> pluginExecute = null;
            var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<Image>>>();

            foreach (var plugin in systemPlugins)
            {
                stepConfigs.AddRange(plugin.PluginProcessingStepConfigs());
                pluginExecute = (provider) => plugin.Execute(provider);

                // Add discovered plugin triggers
                foreach (var stepConfig in stepConfigs)
                {
                    var operation =  stepConfig.Item1.EventOperation;
                    var stage = (ExecutionStage)stepConfig.Item1.ExecutionStage;
                    var trigger = new PluginTrigger(operation, stage, pluginExecute, stepConfig, metadata,stepConfig.Item2.ImpersonatingUserId);

                    AddTrigger(operation, stage, trigger, register);
                }
            }
            SortAllLists(register);
        }

        public void AddTrigger(string operation, ExecutionStage stage, PluginTrigger trigger, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register) {
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
        private void SortAllLists(Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> plugins)
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
        public void Trigger(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core) {

            if (!disableRegisteredPlugins && registeredPlugins.ContainsKey(operation) && registeredPlugins[operation].ContainsKey(stage))
            {
                registeredPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
            }

            if (temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
            {
                temporaryPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
            }
        }
            


        //Post operation - Trigger Sync and Async in that order
        public void TriggerSync(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {

            if (!disableRegisteredPlugins && registeredPlugins.ContainsKey(operation) && registeredPlugins[operation].ContainsKey(stage))
                registeredPlugins[operation][stage].Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .OrderBy(p => p.GetExecutionOrder()).ToList().ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
            if (temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
                temporaryPlugins[operation][stage].Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .OrderBy(p => p.GetExecutionOrder()).ToList().ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }

        public void StageAsync(string operation, ExecutionStage stage,
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

        public void TriggerSystem(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {
            if (!this.registeredSystemPlugins.ContainsKey(operation)) return;
            if (!this.registeredSystemPlugins[operation].ContainsKey(stage)) return;

            registeredSystemPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }

        internal class PluginTrigger : IComparable<PluginTrigger> {
            public Action<MockupServiceProviderAndFactory> pluginExecute;

            string entityName;
            string operation;
            ExecutionStage stage;
            ExecutionMode mode;
            int order = 0;
            Dictionary<string, EntityMetadata> metadata;

            HashSet<string> attributes;
            IEnumerable<Image> images;
            Guid? impersonatingUserId;

            public PluginTrigger(string operation, ExecutionStage stage,
                    Action<MockupServiceProviderAndFactory> pluginExecute, Tuple<StepConfig, ExtendedStepConfig,
                        IEnumerable<Image>> stepConfig, Dictionary<string, EntityMetadata> metadata,Guid? impersonatingUserId) {
                this.pluginExecute = pluginExecute;
                this.entityName = stepConfig.Item1.LogicalName;
                this.operation = operation;
                this.stage = stage;
                this.mode = (ExecutionMode)stepConfig.Item2.ExecutionMode;
                this.order = stepConfig.Item2.ExecutionOrder;
                this.images = stepConfig.Item3;
                this.metadata = metadata;
                if (impersonatingUserId.HasValue && impersonatingUserId != Guid.Empty)
                {
                    this.impersonatingUserId = impersonatingUserId;
                }
                

                var attrs = stepConfig.Item2.FilteredAttributes ?? "";
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
                if (entityName != "" && (operation == "Associate" || operation == "Disassociate"))
                {
                    throw new MockupException(
                        $"An {operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
                }
            }

            private Entity AddPostImageAttributesToEntity(Entity entity, Entity preImage, Entity postImage)
            {
                if (operation == "Update" && stage == ExecutionStage.PostOperation)
                {
                    var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                    entity = entity.CloneEntity();
                    entity.Attributes.AddRange(shadowAddedAttributes);
                }
                return entity;
            }

            private bool FilteredAttributesMatches(Entity entity)
            {
                if (operation != "Update" || attributes.Count == 0)
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

                //override the user id as the user the plugin is running as - the initiating user will still be set to the user who triggered the plugin
                if (this.impersonatingUserId.HasValue)
                {
                    thisPluginContext.UserId = this.impersonatingUserId.Value;
                }

                foreach (var image in this.images)
                {
                    var type = (ImageType)image.ImageType;
                    var cols = image.Attributes != null ? new ColumnSet(image.Attributes.Split(',')) : new ColumnSet(true);
                    if (postImage != null && stage == ExecutionStage.PostOperation && (type == ImageType.PostImage || type == ImageType.Both))
                    {
                        thisPluginContext.PostEntityImages.Add(image.Name, postImage.CloneEntity(metadata.GetMetadata(postImage.LogicalName), cols));
                    }
                    if (preImage != null && type == ImageType.PreImage || type == ImageType.Both)
                    {
                        thisPluginContext.PreEntityImages.Add(image.Name, preImage.CloneEntity(metadata.GetMetadata(preImage.LogicalName), cols));
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
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
using DG.Tools.XrmMockup.Config;

namespace DG.Tools.XrmMockup {

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

        public PluginManager(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins)
        {
            registeredPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            temporaryPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            registeredSystemPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();

            RegisterPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
            RegisterDirectPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
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
                    if (type.BaseType != null && (type.BaseType == basePluginType || (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == basePluginType)))
                    {
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
                    if (!type.IsAbstract && type.GetInterface("IPlugin") == typeof(IPlugin) && type.BaseType == typeof(Object))
                    {
                        RegisterPlugin(type, metadata, plugins, register);
                    }
                }
            }
            SortAllLists(register);
        }


        private void RegisterPlugin(Type basePluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            object plugin = null;
            try
            {
                plugin = Activator.CreateInstance(basePluginType);
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("No parameterless constructor"))
                {
                    throw;
                }
            }

            if (plugin == null)
            {
                return;
            }

            Action<MockupServiceProviderAndFactory> pluginExecute = null;
            var stepConfigs = new List<PluginStepConfig>();
            
            if (basePluginType.GetMethod("PluginProcessingStepConfigs") != null)
            { // Matches DAXIF plugin registration
                stepConfigs.AddRange(
                    basePluginType
                    .GetMethod("PluginProcessingStepConfigs")
                    .Invoke(plugin, new object[] { })
                    as IEnumerable<PluginStepConfig>);
                pluginExecute = (provider) => {
                    basePluginType
                    .GetMethod("Execute")
                    .Invoke(plugin, new object[] { provider });
                };
            }
            else
            { // Retrieve registration from CRM metadata
                var pluginFullName = basePluginType.FullName;

                var metaSteps1 = plugins.Where(x => string.IsNullOrEmpty(x.PluginTypeAssemblyName))
                                       .Where(x => x.AssemblyName == basePluginType.FullName)
                                       .ToList();


                var metaSteps2 = plugins.Where(x => !string.IsNullOrEmpty(x.PluginTypeAssemblyName))
                                       .Where(x => x.AssemblyName == basePluginType.FullName)
                                       .Where(x => x.PluginTypeAssemblyName == basePluginType.GetTypeInfo().Assembly.GetName().Name)
                                       .ToList();

                var metaSteps = metaSteps1.Union(metaSteps2).ToList();

                if (metaSteps == null || metaSteps.Count == 0)
                {
                    throw new MockupException($"Unknown plugin '{basePluginType.FullName}', please use DAXIF registration or make sure the plugin is uploaded to CRM.");
                }
                

                foreach(var metaStep in metaSteps) {
                    var stepConfig = new StepConfig(metaStep.AssemblyName, metaStep.Stage, metaStep.MessageName, metaStep.PrimaryEntity);
                    var extendedConfig = new ExtendedStepConfig(0, metaStep.Mode, metaStep.Name, metaStep.Rank, metaStep.FilteredAttributes, metaStep.ImpersonatingUserId);
                    var imageConfig = metaStep.Images?.Select(x => new ImageConfig(x.Name, x.EntityAlias, x.ImageType, x.Attributes)).ToList() ?? new List<ImageConfig>();
                    stepConfigs.Add(new PluginStepConfig(stepConfig, extendedConfig, imageConfig));
                    pluginExecute = (provider) => {
                        basePluginType
                        .GetMethod("Execute")
                        .Invoke(plugin, new object[] { provider });
                    };

                    if (metaStep.MessageName.ToLower() == "setstatedynamicentity")
                    {
                        var stepConfig2 = new StepConfig(metaStep.AssemblyName, metaStep.Stage, "setstate", metaStep.PrimaryEntity);
                        stepConfigs.Add(new PluginStepConfig(stepConfig2, extendedConfig, imageConfig));
                        pluginExecute = (provider) => {
                            basePluginType
                            .GetMethod("Execute")
                            .Invoke(plugin, new object[] { provider });
                        };
                    }
                }
            }



            // Add discovered plugin triggers
            foreach (var stepConfig in stepConfigs)
            {
                    var stage = (ExecutionStage)stepConfig.StepConfig.ExecutionStage;
                    var trigger = new PluginTrigger(stepConfig.StepConfig.EventOperation, stage, pluginExecute, stepConfig, metadata);
                    AddTrigger(stepConfig.StepConfig.EventOperation, stage, trigger, register);
                
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
            var stepConfigs = new List<PluginStepConfig>();

            foreach (var plugin in systemPlugins)
            {
                stepConfigs.AddRange(plugin.PluginProcessingStepConfigs());
                pluginExecute = (provider) => plugin.Execute(provider);

                // Add discovered plugin triggers
                foreach (var stepConfig in stepConfigs)
                {
                    var stage = (ExecutionStage)stepConfig.StepConfig.ExecutionStage;
                    var trigger = new PluginTrigger(stepConfig.StepConfig.EventOperation, stage, pluginExecute, stepConfig, metadata);

                    AddTrigger(stepConfig.StepConfig.EventOperation, stage, trigger, register);
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
                var toExecute = registeredPlugins[operation][stage].Where(p => p.ShouldExecute(entity, preImage, postImage, pluginContext));
                foreach (var plugin in toExecute)
                {
                    plugin.Execute(entity, preImage, postImage, pluginContext, core);
                }
            }
                
            if (temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
            {
                var toExecute = temporaryPlugins[operation][stage].Where(p => p.ShouldExecute(entity, preImage, postImage, pluginContext));
                foreach (var plugin in toExecute)
                {
                    plugin.Execute(entity, preImage, postImage, pluginContext, core);
                }
            }
        }


        //Post operation - Trigger Sync and Async in that order
        public void TriggerSync(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {

            if (!disableRegisteredPlugins && registeredPlugins.ContainsKey(operation) && registeredPlugins[operation].ContainsKey(stage))
            {
                var toExecute = registeredPlugins[operation][stage].Where(p => p.ShouldExecute(entity, preImage, postImage, pluginContext))
                                                                   .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                                                                   .OrderBy(p => p.GetExecutionOrder());
                foreach (var plugin in toExecute)
                {
                    plugin.Execute(entity, preImage, postImage, pluginContext, core);
                }
            }

            if (temporaryPlugins.ContainsKey(operation) && temporaryPlugins[operation].ContainsKey(stage))
            {
                var toExecute = temporaryPlugins[operation][stage].Where(p => p.ShouldExecute(entity, preImage, postImage, pluginContext))
                                                                       .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                                                                       .OrderBy(p => p.GetExecutionOrder());
                foreach (var plugin in toExecute)
                {
                    plugin.Execute(entity, preImage, postImage, pluginContext, core);
                }
            }
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

            var toExecute = registeredSystemPlugins[operation][stage].Where(p => p.ShouldExecute(entity, preImage, postImage, pluginContext));
                                                                   
            foreach (var plugin in toExecute)
            {
                plugin.Execute(entity, preImage, postImage, pluginContext, core);
            }
        }

        internal class PluginTrigger : IComparable<PluginTrigger> {
            public Action<MockupServiceProviderAndFactory> pluginExecute;

            string entityName;
            string operation;
            ExecutionStage stage;
            ExecutionMode mode;
            int order = 0;
            Dictionary<string, EntityMetadata> metadata;
            Guid? impersonatingUserId;

            HashSet<string> attributes;
            List<ImageConfig> images = new List<ImageConfig>();

            public PluginTrigger(string operation, ExecutionStage stage,
                    Action<MockupServiceProviderAndFactory> pluginExecute, PluginStepConfig stepConfig, Dictionary<string, EntityMetadata> metadata) {
                this.pluginExecute = pluginExecute;
                this.entityName = stepConfig.StepConfig.LogicalName;
                this.operation = operation.ToLower();
                this.stage = stage;
                this.mode = (ExecutionMode)stepConfig.ExtendedStepConfig.ExecutionMode;
                this.order = stepConfig.ExtendedStepConfig.ExecutionOrder;
                if (stepConfig.ImageConfigs != null)
                {
                    this.images.AddRange(stepConfig.ImageConfigs);
                }
                this.metadata = metadata;
                this.impersonatingUserId = stepConfig.ExtendedStepConfig.ImpersonatingUserId;

                var attrs = stepConfig.ExtendedStepConfig.FilteredAttributes ?? "";
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

            public void Execute(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
            {
                // Check if it is supposed to execute. Returns preemptively, if it should not.
                var entity = entityObject as Entity;
                var entityRef = entityObject as EntityReference;

                var guid = (entity != null) ? entity.Id : entityRef.Id;
                var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;

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

            public bool ShouldExecute(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext)
            {
                // Check if it is supposed to execute. Returns preemptively, if it should not.
                var entity = entityObject as Entity;
                var entityRef = entityObject as EntityReference;

                var guid = (entity != null) ? entity.Id : entityRef.Id;
                var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;

                return VerifyPluginTrigger(entity, logicalName, guid, preImage, postImage, pluginContext);
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
                if (entityName != "" && (operation.ToLower() == Enum.GetName(typeof(EventOperation), EventOperation.Associate).ToLower() || operation.ToLower() == Enum.GetName(typeof(EventOperation),EventOperation.Disassociate).ToLower()))
                {
                    throw new MockupException(
                        $"An {operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
                }
            }

            private Entity AddPostImageAttributesToEntity(Entity entity, Entity preImage, Entity postImage)
            {
                if (operation.ToLower() == Enum.GetName(typeof(EventOperation), EventOperation.Update).ToLower() && stage == ExecutionStage.PostOperation)
                {
                    var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                    entity = entity.CloneEntity();
                    entity.Attributes.AddRange(shadowAddedAttributes);
                }
                return entity;
            }

            private bool FilteredAttributesMatches(Entity entity)
            {
                if (operation.ToLower() != Enum.GetName(typeof(EventOperation), EventOperation.Update).ToLower() || attributes.Count == 0)
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
                if (this.impersonatingUserId != null)
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
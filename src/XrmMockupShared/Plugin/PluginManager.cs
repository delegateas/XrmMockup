﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using System.Runtime.ExceptionServices;
using XrmMockupShared.Plugin;

namespace DG.Tools.XrmMockup
{

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes,impersonating user id
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

    internal class PluginManager
    {

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
            new SystemPlugins.DefaultBusinessUnitTeamMembers(),
            new SystemPlugins.SetAnnotationIsDocument()
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
            catch (Exception ex) when (ex.Source == "mscorlib" || ex.Source == "System.Private.CoreLib")
            {
            }

            if (plugin == null)
            {
                return;
            }

            Action<MockupServiceProviderAndFactory> pluginExecute = null;
            var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>();

            if (basePluginType.GetMethod("PluginProcessingStepConfigs") != null)
            { // Matches DAXIF plugin registration

                var configs = basePluginType
                    .GetMethod("PluginProcessingStepConfigs")
                    .Invoke(plugin, new object[] { })
                    as IEnumerable<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>;

                stepConfigs.AddRange(configs);

                pluginExecute = (provider) =>
                {
                    basePluginType
                    .GetMethod("Execute")
                    .Invoke(plugin, new object[] { provider });
                };
            }
            else
            { // Retrieve registration from CRM metadata
                var metaSteps =
                    plugins
                    .Where(x =>
                        x.AssemblyName == basePluginType.FullName &&
                        x.PluginTypeAssemblyName == basePluginType.Assembly.GetName().Name)
                    .ToList();

                // fallback for backwards compatability for old Metadata files
                if (metaSteps == null || metaSteps.Count == 0)
                {
                    metaSteps =
                        plugins
                        .Where(x => x.AssemblyName == basePluginType.FullName)
                        .ToList();
                }

                if (metaSteps == null || metaSteps.Count == 0)
                {
                    throw new MockupException($"Unknown plugin '{basePluginType.FullName}', please use DAXIF registration or make sure the plugin is uploaded to CRM.");
                }

                foreach (var metaStep in metaSteps)
                {
                    var stepConfig = new StepConfig(metaStep.AssemblyName, metaStep.Stage, metaStep.MessageName, metaStep.PrimaryEntity);
                    var extendedConfig = new ExtendedStepConfig(0, metaStep.Mode, metaStep.Name, metaStep.Rank, metaStep.FilteredAttributes, metaStep.ImpersonatingUserId?.ToString());
                    var imageTuple = metaStep.Images?.Select(x => new ImageTuple(x.Name, x.EntityAlias, x.ImageType, x.Attributes)).ToList() ?? new List<ImageTuple>();
                    stepConfigs.Add(new Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>(stepConfig, extendedConfig, imageTuple));
                    pluginExecute = (provider) =>
                    {
                        basePluginType
                        .GetMethod("Execute")
                        .Invoke(plugin, new object[] { provider });
                    };

                    if (metaStep.MessageName.ToLower() == "setstatedynamicentity")
                    {
                        var stepConfig2 = new StepConfig(metaStep.AssemblyName, metaStep.Stage, "setstate", metaStep.PrimaryEntity);
                        stepConfigs.Add(new Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>(stepConfig2, extendedConfig, imageTuple));
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
                var stage = (ExecutionStage)stepConfig.Item1.Item2;
                var trigger = new PluginTrigger(stepConfig.Item1.Item3, stage, pluginExecute, stepConfig, metadata);
                AddTrigger(stepConfig.Item1.Item3.ToLower(), stage, trigger, register);
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
            var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>();

            foreach (var plugin in systemPlugins)
            {
                stepConfigs.AddRange(plugin.PluginProcessingStepConfigs());
                pluginExecute = (provider) => plugin.Execute(provider);

                // Add discovered plugin triggers
                foreach (var stepConfig in stepConfigs)
                {
                    var operation = stepConfig.Item1.Item3.ToLower();
                    var stage = (ExecutionStage)stepConfig.Item1.Item2;
                    var trigger = new PluginTrigger(operation, stage, pluginExecute, stepConfig, metadata);

                    AddTrigger(operation, stage, trigger, register);
                }
            }
            SortAllLists(register);
        }

        public void AddTrigger(string operation, ExecutionStage stage, PluginTrigger trigger, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            if (!register.ContainsKey(operation))
            {
                register.Add(operation, new Dictionary<ExecutionStage, List<PluginTrigger>>());
            }
            if (!register[operation].ContainsKey(stage))
            {
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

        public void TriggerSync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core, Func<PluginTrigger, bool> executionOrderFilter)
        {
            TriggerSyncInternal(operation, stage, entity, preImage, postImage, pluginContext, core, executionOrderFilter);

            // Check if this is a Single -> Multiple request
            if (Mappings.RequestToMultipleRequest.TryGetValue(operation, out var multipleOperation))
            {
                var multiplePluginContext = pluginContext.Clone();
                var entityCollection = new EntityCollection()
                {
                    EntityName = pluginContext.PrimaryEntityName,
                    Entities = { (Entity)entity },
                };

                // Try to get the request type for the multiple
                var multipleRequestType = Mappings.EventOperationToRequest(multipleOperation)
                    ?? throw new MockupException($"Could not find request type for operation {multipleOperation}");

                // Now try to get the image property for the multiple request
                if (!Mappings.EntityImageProperty.TryGetValue(multipleRequestType, out var imageProperty))
                {
                    throw new MockupException($"Could not find image property for operation {multipleOperation} using request type {multipleRequestType}");
                }

                multiplePluginContext.InputParameters[imageProperty] = entityCollection;
                multiplePluginContext.MessageName = Activator.CreateInstance(multipleRequestType) is OrganizationRequest request
                    ? request.RequestName
                    : throw new MockupException($"Could not create request for operation {operation}");

                TriggerSyncInternal(multipleOperation, stage, entityCollection, preImage, postImage, multiplePluginContext, core, executionOrderFilter);
            }

            // Check if this is a Multiple -> Single request
            if (Mappings.SingleOperationFromMultiple(operation) is EventOperation singleOperation)
            {
                // Try to get the request type for the multiple
                var multipleRequestType = Mappings.EventOperationToRequest(operation)
                    ?? throw new MockupException($"Could not find request type for operation {operation}");

                // Now try to get the image property for the multiple request
                if (!Mappings.EntityImageProperty.TryGetValue(multipleRequestType, out var multipleImageProperty))
                {
                    throw new MockupException($"Could not find image property for operation {operation} using request type {multipleRequestType}");
                }

                // Try to get the request type for the single
                var singleRequestType = Mappings.EventOperationToRequest(singleOperation)
                    ?? throw new MockupException($"Could not find request type for operation {singleOperation}");

                // Now try to get the image property for the single request
                if (!Mappings.EntityImageProperty.TryGetValue(singleRequestType, out var singleImageProperty))
                {
                    throw new MockupException($"Could not find image property for operation {singleOperation} using request type {singleRequestType}");
                }

                // Get the message for the single message
                var singleMessageName = Activator.CreateInstance(singleRequestType) is OrganizationRequest request
                    ? request.RequestName
                    : throw new MockupException($"Could not create request for operation {operation}");

                var targets = pluginContext.InputParameters[multipleImageProperty] as EntityCollection;

                foreach (var targetEntity in targets.Entities)
                {
                    var singlePluginContext = pluginContext.Clone();
                    singlePluginContext.InputParameters[singleImageProperty] = targetEntity;
                    singlePluginContext.MessageName = singleMessageName;
                    
                    TriggerSyncInternal(singleOperation, stage, targetEntity, preImage, postImage, singlePluginContext, core, executionOrderFilter);
                }
            }
        }

        private void TriggerSyncInternal(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core, Func<PluginTrigger, bool> executionOrderFilter)
        {
            var operationName = operation.ToString().ToLower();
            if (!disableRegisteredPlugins && registeredPlugins.TryGetValue(operationName, out var operationPlugins) && operationPlugins.TryGetValue(stage, out var stagePlugins))
                stagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .Where(executionOrderFilter)
                    .OrderBy(p => p.GetExecutionOrder())
                    .ToList()
                    .ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));

            if (temporaryPlugins.TryGetValue(operationName, out var tempOperationPlugins) && tempOperationPlugins.TryGetValue(stage, out var tempStagePlugins))
                tempStagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .Where(executionOrderFilter)
                    .OrderBy(p => p.GetExecutionOrder())
                    .ToList()
                    .ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }

        public void StageAsync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
        {
            var operationName = operation.ToString().ToLower();
            if (!disableRegisteredPlugins && registeredPlugins.TryGetValue(operationName, out var operationPlugins) && operationPlugins.TryGetValue(stage, out var stagePlugins))
                stagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder())
                    .Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, core))
                    .ToList()
                    .ForEach(pendingAsyncPlugins.Enqueue);

            if (temporaryPlugins.TryGetValue(operationName, out var tempOperationPlugins) && tempOperationPlugins.TryGetValue(stage, out var tempStagePlugins))
                tempStagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder())
                    .Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, core))
                    .ToList()
                    .ForEach(pendingAsyncPlugins.Enqueue);
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
            var operationName = operation.ToString().ToLower();

            if (!registeredSystemPlugins.TryGetValue(operationName, out var stagePlugins))
            {
                return;
            }

            if (!stagePlugins.TryGetValue(stage, out var plugins))
            {
                return;
            }

            plugins.ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }

        internal class PluginTrigger : IComparable<PluginTrigger>
        {
            public Action<MockupServiceProviderAndFactory> pluginExecute;

            string entityName;
            string operation;
            ExecutionStage stage;
            ExecutionMode mode;
            int order = 0;
            Dictionary<string, EntityMetadata> metadata;
            string impersonatingUserId;

            HashSet<string> attributes;
            IEnumerable<ImageTuple> images;

            public PluginTrigger(string operation, ExecutionStage stage,
                    Action<MockupServiceProviderAndFactory> pluginExecute, Tuple<StepConfig, ExtendedStepConfig,
                        IEnumerable<ImageTuple>> stepConfig, Dictionary<string, EntityMetadata> metadata)
            {
                this.pluginExecute = pluginExecute;
                this.entityName = stepConfig.Item1.Item4;
                this.operation = operation.ToLower();
                this.stage = stage;
                this.mode = (ExecutionMode)stepConfig.Item2.Item2;
                this.order = stepConfig.Item2.Item4;
                this.images = stepConfig.Item3;
                this.metadata = metadata;
                this.impersonatingUserId = stepConfig.Item2.Item6;

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

                if (VerifyPluginTrigger(entity, logicalName, guid, preImage, postImage, pluginContext))
                {
                    // Create the plugin context
                    var thisPluginContext = CreatePluginContext(pluginContext, guid, logicalName, preImage, postImage);
                    return new PluginExecutionProvider(pluginExecute, new MockupServiceProviderAndFactory(core, thisPluginContext, core.TracingServiceFactory));
                }

                return null;
            }

            public void ExecuteIfMatch(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, Core core)
            {
                // Check if it is supposed to execute. Returns preemptively, if it should not.
                var entity = entityObject as Entity;
                var entityRef = entityObject as EntityReference;
                var entityCollection = entityObject as EntityCollection;

                var guid = 
                    entity != null
                    ? entity.Id : 
                    entityRef != null 
                    ? entityRef.Id
                    : Guid.Empty;
                var logicalName = 
                    entity != null 
                    ? entity.LogicalName : 
                    entityRef != null 
                    ? entityRef.LogicalName
                    : entityCollection.EntityName;

                if (VerifyPluginTrigger(entity, logicalName, guid, preImage, postImage, pluginContext))
                {
                    var thisPluginContext = CreatePluginContext(pluginContext, guid, logicalName, preImage, postImage);

                    //Create Serviceprovider, and execute plugin
                    MockupServiceProviderAndFactory provider = new MockupServiceProviderAndFactory(core, thisPluginContext, core.TracingServiceFactory);
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
                if (entityName != "" && (operation == nameof(EventOperation.Associate).ToLower() || operation == nameof(EventOperation.Disassociate).ToLower()))
                {
                    throw new MockupException(
                        $"An {operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
                }
            }

            private Entity AddPostImageAttributesToEntity(Entity entity, Entity preImage, Entity postImage)
            {
                if (operation == nameof(EventOperation.Update).ToLower() && stage == ExecutionStage.PostOperation)
                {
                    var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                    entity = entity.CloneEntity();
                    entity.Attributes.AddRange(shadowAddedAttributes);
                }
                return entity;
            }

            private bool FilteredAttributesMatches(Entity entity)
            {
                if (operation != nameof(EventOperation.Update).ToLower() || attributes.Count == 0)
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
                entity = AddPostImageAttributesToEntity(entity, preImage, postImage);
                CheckSpecialRequest();

                if (FilteredAttributesMatches(entity))
                {
                    return true;
                }
                return false;
            }

            private PluginContext CreatePluginContext(PluginContext pluginContext, Guid guid, string logicalName, Entity preImage, Entity postImage)
            {
                var thisPluginContext = pluginContext.Clone();
                thisPluginContext.Mode = (int)this.mode;
                thisPluginContext.Stage = (int)this.stage;
                if (thisPluginContext.PrimaryEntityId == Guid.Empty)
                {
                    thisPluginContext.PrimaryEntityId = guid;
                }
                thisPluginContext.PrimaryEntityName = logicalName;
                if (Guid.TryParse(this.impersonatingUserId, out Guid impersonatingUserId) && impersonatingUserId != Guid.Empty)
                {
                    thisPluginContext.UserId = impersonatingUserId;
                }

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

            public int CompareTo(PluginTrigger other)
            {
                return this.order.CompareTo(other.order);
            }
        }
    }
}

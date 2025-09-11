using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

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
        // Static caches shared across all PluginManager instances
        private static readonly ConcurrentDictionary<string, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>> _cachedRegisteredPlugins = new ConcurrentDictionary<string, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>>();
        private static readonly ConcurrentDictionary<string, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>> _cachedSystemPlugins = new ConcurrentDictionary<string, Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>>();
        private static readonly ConcurrentDictionary<Type, object> _pluginInstanceCache = new ConcurrentDictionary<Type, object>();
        private static readonly object _cacheLock = new object();

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
            temporaryPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();

            var pluginCacheKey = GeneratePluginCacheKey(basePluginTypes);
            var systemCacheKey = "system_plugins";

            // Check if we have cached results
            if (_cachedRegisteredPlugins.ContainsKey(pluginCacheKey) && _cachedSystemPlugins.ContainsKey(systemCacheKey))
            {
                // Use cached results - no reflection/instantiation needed
                registeredPlugins = ClonePluginDictionary(_cachedRegisteredPlugins[pluginCacheKey]);
                registeredSystemPlugins = ClonePluginDictionary(_cachedSystemPlugins[systemCacheKey]);
            }
            else
            {
                lock (_cacheLock)
                {
                    // Double-check locking pattern
                    if (_cachedRegisteredPlugins.ContainsKey(pluginCacheKey) && _cachedSystemPlugins.ContainsKey(systemCacheKey))
                    {
                        registeredPlugins = ClonePluginDictionary(_cachedRegisteredPlugins[pluginCacheKey]);
                        registeredSystemPlugins = ClonePluginDictionary(_cachedSystemPlugins[systemCacheKey]);
                    }
                    else
                    {
                        // First time - do the work and cache it
                        registeredPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
                        registeredSystemPlugins = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();

                        RegisterPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
                        RegisterDirectPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
                        RegisterSystemPlugins(registeredSystemPlugins, metadata);

                        // Cache for future instances
                        _cachedRegisteredPlugins[pluginCacheKey] = ClonePluginDictionary(registeredPlugins);
                        _cachedSystemPlugins[systemCacheKey] = ClonePluginDictionary(registeredSystemPlugins);
                    }
                }
            }
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
            object plugin = _pluginInstanceCache.GetOrAdd(basePluginType, type =>
            {
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch (Exception ex) when (ex.Source == "mscorlib" || ex.Source == "System.Private.CoreLib")
                {
                    return null;
                }
            });

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

        private string GeneratePluginCacheKey(IEnumerable<Type> basePluginTypes)
        {
            if (basePluginTypes == null) return "null_types";
            
            var typeNames = basePluginTypes.Where(t => t != null).Select(t => t.FullName).OrderBy(n => n);
            var combinedTypes = string.Join("|", typeNames);
            
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedTypes));
                return Convert.ToBase64String(hash);
            }
        }

        private Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> ClonePluginDictionary(Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>> source)
        {
            var result = new Dictionary<string, Dictionary<ExecutionStage, List<PluginTrigger>>>();
            foreach (var operationEntry in source)
            {
                var stageDict = new Dictionary<ExecutionStage, List<PluginTrigger>>();
                foreach (var stageEntry in operationEntry.Value)
                {
                    stageDict[stageEntry.Key] = new List<PluginTrigger>(stageEntry.Value);
                }
                result[operationEntry.Key] = stageDict;
            }
            return result;
        }
    }
}

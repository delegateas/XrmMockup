using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;
using DG.Tools.XrmMockup.SystemPlugins;
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using XrmMockupShared.Plugin;

namespace DG.Tools.XrmMockup
{
    internal class PluginManager
    {
        // Static caches shared across all PluginManager instances
        private static readonly ConcurrentDictionary<string, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>> _cachedRegisteredPlugins = new ConcurrentDictionary<string, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>>();
        private static readonly ConcurrentDictionary<string, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>> _cachedSystemPlugins = new ConcurrentDictionary<string, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>>();
        private static readonly ConcurrentDictionary<Type, IPlugin> _pluginInstanceCache = new ConcurrentDictionary<Type, IPlugin>();
        private static readonly object _cacheLock = new object();

        // TODO: We can probably optimize lookup by creating a key record and using that instead of a Dictionary of Dictionaries
        private readonly Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
        private readonly Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> temporaryPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
        private readonly Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredSystemPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();

        // Queue for AsyncPlugins
        private readonly Queue<PluginExecutionProvider> pendingAsyncPlugins = new Queue<PluginExecutionProvider>();

        // If true, registered plugins will not be executed
        private bool disableRegisteredPlugins = false;

        // List of SystemPlugins to execute
        private readonly List<AbstractSystemPlugin> systemPlugins = new List<AbstractSystemPlugin>
        {
            new UpdateInactiveIncident(),
            new DefaultBusinessUnitTeams(),
            new DefaultBusinessUnitTeamMembers(),
            new SetAnnotationIsDocument()
        };

        private readonly List<IRegistrationStrategy<IPluginStepConfig>> registrationStrategies = new List<IRegistrationStrategy<IPluginStepConfig>>
        {
            new Plugin.RegistrationStrategy.XrmPluginCore.PluginRegistrationStrategy(),
            new Plugin.RegistrationStrategy.DAXIF.PluginRegistrationStrategy()
        };

        public PluginManager(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins)
        {
            temporaryPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();

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
                        registeredPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
                        registeredSystemPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();

                        // TODO: Find all concrete types that implement IPlugin, handle system plugins separately
                        // TODO: How do we filter CustomAPIs?
                        // TODO: Should basePluginTypes act as an optional filter?

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

        internal List<string> PluginRegistrations => FlattenPluginDictionary(registeredPlugins);
        internal List<string> TemporaryPluginRegistrations => FlattenPluginDictionary(temporaryPlugins);
        internal List<string> SystemPluginRegistrations => FlattenPluginDictionary(registeredSystemPlugins);

        private static List<string> FlattenPluginDictionary(Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> plugins) => plugins
            .SelectMany(kvpOperation => kvpOperation.Value.SelectMany(kvpStage => kvpStage.Value.Select(z => $"{kvpOperation.Key} {kvpStage.Key} {z.EntityName ?? "AnyEntity"}: {z.PluginExecute.Target.GetType().Name}")))
            .ToList();

        private void RegisterPlugins(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            foreach (var basePluginType in basePluginTypes)
            {
                if (basePluginType == null) continue;

                // Look for any currently loaded types in the AppDomain that implement the base type
                var pluginTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetLoadableTypes().Where(t => !t.IsAbstract && t.IsPublic && t.BaseType != null && (t.BaseType == basePluginType || (t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == basePluginType))));

                foreach (var type in pluginTypes)
                {
                    RegisterPlugin(type, metadata, plugins, register);
                }
            }
            SortAllLists(register);
        }

        private void RegisterDirectPlugins(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            if (basePluginTypes == null) return;

            foreach (var pluginType in basePluginTypes)
            {
                if (pluginType == null) continue;

                Assembly proxyTypeAssembly = pluginType.Assembly;

                // Look for any currently loaded types in assembly that implement IPlugin
                var types = proxyTypeAssembly.GetLoadableTypes()
                    .Where(t => t.BaseType == typeof(object) && !t.IsAbstract && t.IsPublic && typeof(IPlugin).IsAssignableFrom(t));

                foreach (var type in types)
                {
                    RegisterPlugin(type, metadata, plugins, register);
                }
            }
            SortAllLists(register);
        }

        private void RegisterPlugin(Type pluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> metaPlugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            var plugin = _pluginInstanceCache.GetOrAdd(pluginType, Utility.CreatePluginInstance);
            if (plugin == null)
            {
                return;
            }

            // Get the plugin step registrations from the plugin type
            // and add discovered plugin triggers
            var triggers = registrationStrategies
                .SelectMany(s => s.AnalyzeType(plugin))
                .Concat(new MetadataRegistrationStrategy().AnalyzeType(pluginType, metaPlugins))
                .Select(t => new PluginTrigger(t.EventOperation, t.ExecutionStage, plugin.Execute, t, metadata));

            if (!triggers.Any())
            {
                throw new MockupException($"No plugin step registrations found for plugin '{pluginType.FullName}', please use XrmPluginCore, DAXIF registration, or make sure the plugin is uploaded to CRM and metadata has been updated.");
            }

            foreach (var trigger in triggers)
            {
                AddTrigger(trigger, register);
            }
        }

        public void ResetPlugins()
        {
            disableRegisteredPlugins = false;
            temporaryPlugins.Clear();
        }

        public void DisableRegisteredPlugins(bool disable)
        {
            disableRegisteredPlugins = disable;
        }

        public void RegisterAdditionalPlugin(Type pluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, PluginRegistrationScope scope)
        {
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
            var registrations = new List<IPluginStepConfig>();

            // TODO: Auto-discover system plugins instead of hardcoding the types, we know they all implement the same class
            foreach (var plugin in systemPlugins)
            {
                registrations.AddRange(plugin.GetRegistrations());

                // Add discovered plugin triggers
                foreach (var registration in registrations)
                {
                    var trigger = new PluginTrigger(registration.EventOperation, registration.ExecutionStage, plugin.Execute, registration, metadata);
                    AddTrigger(trigger, register);
                }
            }

            SortAllLists(register);
        }

        private static void AddTrigger(PluginTrigger trigger, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            var operation = trigger.Operation;
            var stage = trigger.Stage;

            if (!register.TryGetValue(operation, out var operationRegister))
            {
                register[operation] = operationRegister = new Dictionary<ExecutionStage, List<PluginTrigger>>();
            }

            if (!operationRegister.TryGetValue(stage, out var stageRegister))
            {
                operationRegister[stage] = stageRegister = new List<PluginTrigger>();
            }

            stageRegister.Add(trigger);
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

                // TODO: Images for multiple are handled in IPluginExecutionContext4
                TriggerSyncInternal(multipleOperation, stage, entityCollection, null, null, multiplePluginContext, core, executionOrderFilter);
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
                    
                    // TODO: Recalculate preImage and postImage here
                    TriggerSyncInternal(singleOperation, stage, targetEntity, preImage, postImage, singlePluginContext, core, executionOrderFilter);
                }
            }
        }

        private void TriggerSyncInternal(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Core core, Func<PluginTrigger, bool> executionOrderFilter)
        {
            if (!disableRegisteredPlugins && registeredPlugins.TryGetValue(operation, out var operationPlugins) && operationPlugins.TryGetValue(stage, out var stagePlugins))
                stagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .Where(executionOrderFilter)
                    .OrderBy(p => p.GetExecutionOrder())
                    .ToList()
                    .ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));

            if (temporaryPlugins.TryGetValue(operation, out var tempOperationPlugins) && tempOperationPlugins.TryGetValue(stage, out var tempStagePlugins))
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
            if (!disableRegisteredPlugins && registeredPlugins.TryGetValue(operation, out var operationPlugins) && operationPlugins.TryGetValue(stage, out var stagePlugins))
                stagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder())
                    .Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, core))
                    .ToList()
                    .ForEach(pendingAsyncPlugins.Enqueue);

            if (temporaryPlugins.TryGetValue(operation, out var tempOperationPlugins) && tempOperationPlugins.TryGetValue(stage, out var tempStagePlugins))
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
            if (!registeredSystemPlugins.TryGetValue(operation, out var stagePlugins))
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

        private Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> ClonePluginDictionary(Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> source)
        {
            var result = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
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

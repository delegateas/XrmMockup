using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockupShared.Plugin;
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.Plugin;
using DG.Tools.XrmMockup.SystemPlugins;
using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;

namespace DG.Tools.XrmMockup
{
    internal class PluginManager
    {
        private readonly Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
        private readonly Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> temporaryPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();
        private readonly Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredSystemPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();

        // Queue for AsyncPlugins
        private readonly Queue<PluginExecutionProvider> pendingAsyncPlugins = new Queue<PluginExecutionProvider>();

        private bool disableRegisteredPlugins = false;

        // List of SystemPlugins to execute
        private readonly List<SystemPluginBase> systemPlugins = new List<SystemPluginBase>
        {
            new UpdateInactiveIncident(),
            new DefaultBusinessUnitTeams(),
            new DefaultBusinessUnitTeamMembers(),
            new SetAnnotationIsDocument()
        };

        private readonly List<IRegistrationStrategy> registrationStrategies = new List<IRegistrationStrategy>
        {
            new XrmPluginCoreRegistrationStrategy(),
            new LegacyRegistrationStrategy(),
            new MetadataRegistrationStrategy()
        };

        public PluginManager(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins)
        {
            RegisterPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
            RegisterDirectPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
            RegisterSystemPlugins(registeredSystemPlugins, metadata);
        }

        private void RegisterPlugins(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            foreach (var basePluginType in basePluginTypes)
            {
                if (basePluginType == null) continue;

                // Look for any currently loaded types in the AppDomain that implement the base type
                var pluginTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetLoadableTypes()
                        .Where(t => t.BaseType != null && (t.BaseType == basePluginType || (t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == basePluginType))))
                    .ToList();

                pluginTypes.ForEach(type => RegisterPlugin(type, metadata, plugins, register));
            }
            SortAllLists(register);
        }

        private void RegisterDirectPlugins(IEnumerable<Type> pluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            if (pluginTypes == null) return;

            foreach (var pluginType in pluginTypes)
            {
                if (pluginType == null) continue;
                Assembly proxyTypeAssembly = pluginType.Assembly;

                foreach (var type in proxyTypeAssembly.GetLoadableTypes())
                {
                    if (!type.IsAbstract && type.GetInterface(nameof(IPlugin)) == typeof(IPlugin) && type.BaseType == typeof(object))
                    {
                        RegisterPlugin(type, metadata, plugins, register);
                    }
                }
            }
            SortAllLists(register);
        }


        private void RegisterPlugin(Type pluginType, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> register)
        {
            var plugin = Utility.CreatePluginInstance(pluginType);
            if (plugin == null)
            {
                return;
            }

            // Filter the known strategies, with fallback to the metadata strategy
            var relevantStrategy = registrationStrategies.FirstOrDefault(r => r.IsValidForPlugin(pluginType))
                ?? throw new MockupException($"No relevant registration strategy found for plugin '{pluginType.FullName}'.");

            // Get the plugin step registrations from the plugin type
            // and add discovered plugin triggers
            var triggers = relevantStrategy
                .GetPluginRegistrations(pluginType, plugin, plugins)
                .Select(registration => new PluginTrigger(registration.EventOperation, registration.ExecutionStage, plugin.Execute, registration, metadata));

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
    }
}

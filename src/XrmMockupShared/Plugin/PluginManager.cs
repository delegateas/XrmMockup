using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools {

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = System.Tuple<string, int, string, string>;
    using ExtendedStepConfig = System.Tuple<int, int, string, int, string, string>;
    using ImageTuple = System.Tuple<string, string, int, string>;
    using Domain;
    using System.ServiceModel;
    using Microsoft.Xrm.Sdk.Metadata;

    internal class PluginManager {

        private Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>> registeredPlugins;

        public PluginManager(IEnumerable<Type> basePluginTypes, Dictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins) {
            registeredPlugins = new Dictionary<EventOperation, Dictionary<ExecutionStage, List<PluginTrigger>>>();

            foreach (var basePluginType in basePluginTypes) {
                if (basePluginType == null) continue;
                Assembly proxyTypeAssembly = basePluginType.Assembly;

                foreach (var type in proxyTypeAssembly.GetLoadableTypes()) {
                    if (type.BaseType != basePluginType) continue;
                    var plugin = Activator.CreateInstance(type);

                    Action<MockupServiceProviderAndFactory> pluginExecute = null;
                    var stepConfigs = new List<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>();

                    if (basePluginType.GetMethod("PluginProcessingStepConfigs") != null) { // Matches DAXIF plugin registration
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

                    } else { // Retrieve registration from CRM metadata
                        var metaPlugin = plugins.FirstOrDefault(x => x.AssemblyName == type.FullName);
                        if (metaPlugin == null) {
                            throw new MockupException($"Unknown plugin '{type.FullName}', please use DAXIF registration or make sure the plugin is uploaded to CRM.");
                        }
                        var stepConfig = new StepConfig(metaPlugin.AssemblyName, metaPlugin.Stage, metaPlugin.MessageName, metaPlugin.PrimaryEntity);
                        var extendedConfig = new ExtendedStepConfig(0, metaPlugin.Mode, metaPlugin.Name, metaPlugin.Rank, metaPlugin.FilteredAttributes, Guid.Empty.ToString());
                        var imageTuple = new List<ImageTuple>();
                        stepConfigs.Add(new Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>(stepConfig, extendedConfig, imageTuple));
                        pluginExecute = (provider) => {
                            type
                            .GetMethod("Execute")
                            .Invoke(plugin, new object[] { provider });
                        };
                    }

                    // Add discovered plugin triggers
                    foreach (var stepConfig in stepConfigs) {
                        var operation = (EventOperation)Enum.Parse(typeof(EventOperation), stepConfig.Item1.Item3);
                        var stage = (ExecutionStage)stepConfig.Item1.Item2;
                        var trigger = new PluginTrigger(operation, stage, pluginExecute, stepConfig, metadata);

                        AddTrigger(operation, stage, trigger);
                    }
                }
            }
            SortAllLists();
        }

        private void AddTrigger(EventOperation operation, ExecutionStage stage, PluginTrigger trigger) {
            if (!registeredPlugins.ContainsKey(operation)) {
                registeredPlugins.Add(operation, new Dictionary<ExecutionStage, List<PluginTrigger>>());
            }
            if (!registeredPlugins[operation].ContainsKey(stage)) {
                registeredPlugins[operation].Add(stage, new List<PluginTrigger>());
            }
            registeredPlugins[operation][stage].Add(trigger);
        }

        /// <summary>
        /// Sorts all the registered which shares the same entry point based on their given order
        /// </summary>
        private void SortAllLists() {
            foreach (var dictEntry in registeredPlugins) {
                foreach (var listEntry in dictEntry.Value) {
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
        /// <param name="crm"></param>
        public void Trigger(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, XrmMockupBase crm) {
            if (!this.registeredPlugins.ContainsKey(operation)) return;
            if (!this.registeredPlugins[operation].ContainsKey(stage)) return;

            registeredPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, crm));
        }


        class PluginTrigger : IComparable<PluginTrigger> {
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

            public void ExecuteIfMatch(object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext, XrmMockupBase crm) {
                // Check if it is supposed to execute. Returns preemptively, if it should not.
                var entity = entityObject as Entity;
                var entityRef = entityObject as EntityReference;

                var guid = (entity != null) ? entity.Id : entityRef.Id;
                var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;
                if (entityName != "" && entityName != logicalName) return;

                if (pluginContext.Depth > 8) {
                    throw new FaultException(
                        "This workflow job was canceled because the workflow that started it included an infinite loop." +
                        " Correct the workflow logic and try again.");
                }

                if (operation == EventOperation.Update && stage == ExecutionStage.PostOperation) {
                    var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                    entity = entity.CloneEntity();
                    entity.Attributes.AddRange(shadowAddedAttributes);
                }

                if (operation == EventOperation.Update && attributes.Count > 0) {
                    var foundAttr = false;
                    foreach (var attr in entity.Attributes) {
                        if (attributes.Contains(attr.Key)) {
                            foundAttr = true;
                            break;
                        }
                    }
                    if (!foundAttr) return;
                }

                if (entityName != "" && (operation == EventOperation.Associate || operation == EventOperation.Disassociate)) {
                    throw new MockupException($"An {operation.ToString()} plugin was registered for a specific entity, can only" +
                        " be registered on AnyEntity");
                }

                // Create the plugin context
                var thisPluginContext = pluginContext.Clone();
                thisPluginContext.Mode = (int)this.mode;
                thisPluginContext.Stage = (int)this.stage;
                thisPluginContext.PrimaryEntityId = guid;
                thisPluginContext.PrimaryEntityName = logicalName;

                foreach (var image in this.images) {
                    var type = (ImageType)image.Item3;
                    var cols = image.Item4 != null ? new ColumnSet(image.Item4.Split(',')) : new ColumnSet(true);
                    if (postImage != null && stage == ExecutionStage.PostOperation && (type == ImageType.PostImage || type == ImageType.Both)) {
                        thisPluginContext.PostEntityImages.Add(image.Item1, postImage.CloneEntity(metadata.GetMetadata(postImage.LogicalName), cols));
                    }
                    if (preImage != null && type == ImageType.PreImage || type == ImageType.Both) {
                        thisPluginContext.PreEntityImages.Add(image.Item1, preImage.CloneEntity(metadata.GetMetadata(preImage.LogicalName), cols));
                    }
                }

                // Create service provider and execute the plugin
                MockupServiceProviderAndFactory provider = new MockupServiceProviderAndFactory(crm, thisPluginContext, new TracingService());
                try {
                    pluginExecute(provider);
                } catch (TargetInvocationException e) {
                    throw e.InnerException;
                }
            }

            public int CompareTo(PluginTrigger other) {
                return this.order.CompareTo(other.order);
            }
        }
    }
}

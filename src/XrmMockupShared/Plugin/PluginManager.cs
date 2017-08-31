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

namespace DG.Tools.XrmMockup.Plugin {

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

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
        /// <param name="core"></param>
        public void Trigger(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, MockupPluginContext pluginContext, Core core) {
            if (!this.registeredPlugins.ContainsKey(operation)) return;
            if (!this.registeredPlugins[operation].ContainsKey(stage)) return;

            registeredPlugins[operation][stage].ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, core));
        }
        
    }
}

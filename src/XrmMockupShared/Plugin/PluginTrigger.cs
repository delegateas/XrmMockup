using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.Plugin {

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

    internal class PluginTrigger : IComparable<PluginTrigger> {
        internal Action<MockupServiceProviderAndFactory> pluginExecute;

        string entityName;
        EventOperation operation;
        ExecutionStage stage;
        ExecutionMode mode;
        int order = 0;
        Dictionary<string, EntityMetadata> metadata;

        HashSet<string> filteredAttributes;
        IEnumerable<ImageTuple> images;

        public PluginTrigger(
            EventOperation operation,
            ExecutionStage stage,
            Action<MockupServiceProviderAndFactory> pluginExecute,
            Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>> stepConfig,
            Dictionary<string, EntityMetadata> metadata)
            : this(
                  operation, stage, pluginExecute,
                  stepConfig.Item1.Item4, (ExecutionMode)stepConfig.Item2.Item2, stepConfig.Item2.Item4,
                  stepConfig.Item3, stepConfig.Item2.Item5, metadata) {
        }

        public PluginTrigger(
            EventOperation operation,
            ExecutionStage stage,
            Action<MockupServiceProviderAndFactory> pluginExecute,
            string entityName = "",
            ExecutionMode mode = ExecutionMode.Synchronous,
            int order = 0,
            IEnumerable<ImageTuple> images = null,
            string filteredAttributesString = null,
            Dictionary<string, EntityMetadata> metadata = null) {

            this.pluginExecute = pluginExecute;
            this.entityName = entityName;
            this.operation = operation;
            this.stage = stage;
            this.mode = mode;
            this.order = order;
            this.images = images;
            this.metadata = metadata;

            this.filteredAttributes =
                String.IsNullOrWhiteSpace(filteredAttributesString)
                ? new HashSet<string>()
                : new HashSet<string>(filteredAttributesString.Split(','));
        }

        internal void ExecuteIfMatch(object entityObject, Entity preImage, Entity postImage, MockupPluginContext pluginContext, Core core) {
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

            if (operation == EventOperation.Update && filteredAttributes.Count > 0) {
                var foundAttr = false;
                foreach (var attr in entity.Attributes) {
                    if (filteredAttributes.Contains(attr.Key)) {
                        foundAttr = true;
                        break;
                    }
                }
                if (!foundAttr) return;
            }

            if (entityName != "" && (operation == EventOperation.Associate || operation == EventOperation.Disassociate)) {
                throw new MockupException(
                    $"An {operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
            }

            // Create the plugin context
            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Mode = (int)this.mode;
            thisPluginContext.Stage = (int)this.stage;
            if (thisPluginContext.PrimaryEntityId == Guid.Empty) {
                thisPluginContext.PrimaryEntityId = guid;
            }
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
            var provider = new MockupServiceProviderAndFactory(core, thisPluginContext, new TracingService());
            try {
                pluginExecute(provider);
            } catch (TargetInvocationException e) {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        public int CompareTo(PluginTrigger other) {
            return this.order.CompareTo(other.order);
        }
    }
}

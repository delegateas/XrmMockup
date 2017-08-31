using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace DG.Tools.XrmMockup.Plugin {


    public class PluginStepRegistration {
        private Type pluginType;
        private IPlugin pluginInstance;
        public IPlugin PluginInstance {
            get {
                if (pluginInstance == null) pluginInstance = Activator.CreateInstance(pluginType) as IPlugin;
                return PluginInstance;
            }
        }

        public string EntityLogicalName;
        public ExecutionStage ExecutionStage;
        public EventOperation EventOperation;
        public ExecutionMode ExecutionMode;
        public Deployment Deployment;
        public int ExecutionOrder;
        public HashSet<string> FilteredAttributes;

        public IEnumerable<PluginImageRegistration> Images;

        private PluginStepRegistration(IPlugin pluginInstance, string entityLogicalName, ExecutionStage stage, EventOperation operation) {
            if (pluginInstance == null) {
                throw new MockupException("You need to specify a plugin instance when using this constructor.");
            }
            this.pluginInstance = pluginInstance;
            this.EntityLogicalName = entityLogicalName;
            this.ExecutionStage = stage;
            this.EventOperation = operation;
        }

        private PluginStepRegistration(Type pluginType, string entityLogicalName, ExecutionStage stage, EventOperation operation) {
            if (typeof(IPlugin).IsAssignableFrom(pluginType)) {
                throw new MockupException("The given plugin type does not implement the IPlugin-interface.");
            }
            this.pluginType = pluginType;
            this.EntityLogicalName = entityLogicalName;
            this.ExecutionStage = stage;
            this.EventOperation = operation;
        }
        

        public static PluginStepRegistration New(IPlugin pluginType, string entityLogicalName, ExecutionStage stage, EventOperation operation) {
            return new PluginStepRegistration(pluginType, entityLogicalName, stage, operation);
        }

        public static PluginStepRegistration New<T>(IPlugin pluginType, ExecutionStage stage, EventOperation operation) where T : Entity {
            var instance = Activator.CreateInstance<T>() as Entity;
            return new PluginStepRegistration(pluginType, instance.LogicalName, stage, operation);
        }

        internal void ExecuteIfMatch(object entityObject, Entity preImage, Entity postImage, MockupPluginContext pluginContext, Core core) {
            // Check if it is supposed to execute. Returns preemptively, if it should not.
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = (entity != null) ? entity.Id : entityRef.Id;
            var logicalName = (entity != null) ? entity.LogicalName : entityRef.LogicalName;
            if (!String.IsNullOrEmpty(EntityLogicalName) && EntityLogicalName != logicalName) return;

            if (pluginContext.Depth > 8) {
                throw new FaultException(
                    "This workflow job was canceled because the workflow that started it included an infinite loop." +
                    " Correct the workflow logic and try again.");
            }

            if (EventOperation == EventOperation.Update && ExecutionStage == ExecutionStage.PostOperation) {
                var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                entity = entity.CloneEntity();
                entity.Attributes.AddRange(shadowAddedAttributes);
            }

            if (EventOperation == EventOperation.Update && FilteredAttributes.Count > 0) {
                var foundAttr = false;
                foreach (var attr in entity.Attributes) {
                    if (FilteredAttributes.Contains(attr.Key)) {
                        foundAttr = true;
                        break;
                    }
                }
                if (!foundAttr) return;
            }

            if (!String.IsNullOrEmpty(EntityLogicalName) && (EventOperation == EventOperation.Associate || EventOperation == EventOperation.Disassociate)) {
                throw new MockupException(
                    $"An {EventOperation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
            }

            // Create the plugin context
            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Mode = (int)this.ExecutionMode;
            thisPluginContext.Stage = (int)this.ExecutionStage;
            if (thisPluginContext.PrimaryEntityId == Guid.Empty) {
                thisPluginContext.PrimaryEntityId = guid;
            }
            thisPluginContext.PrimaryEntityName = logicalName;

            foreach (var image in this.Images) {
                var type = image.ImageType;
                var cols = image.Attributes != null ? new ColumnSet(image.Attributes.ToArray()) : new ColumnSet(true);
                if (postImage != null && ExecutionStage == ExecutionStage.PostOperation && (type == ImageType.PostImage || type == ImageType.Both)) {
                    thisPluginContext.PostEntityImages.Add(image.Name, postImage.CloneEntity(null, cols));
                }
                if (preImage != null && type == ImageType.PreImage || type == ImageType.Both) {
                    thisPluginContext.PreEntityImages.Add(image.Name, preImage.CloneEntity(null, cols));
                }
            }

            // Create service provider and execute the plugin
            var provider = new MockupServiceProviderAndFactory(core, thisPluginContext, new TracingService());
            try {
                PluginInstance.Execute(provider);
            } catch (TargetInvocationException e) {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        public int CompareTo(PluginStepRegistration other) {
            return this.ExecutionOrder.CompareTo(other.ExecutionOrder);
        }
    }
}
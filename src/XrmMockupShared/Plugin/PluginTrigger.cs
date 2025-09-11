using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockupShared.Plugin;

namespace DG.Tools.XrmMockup
{
    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes,impersonating user id
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

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
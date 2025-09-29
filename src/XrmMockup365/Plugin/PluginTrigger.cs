using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using System.Runtime.ExceptionServices;
using XrmMockupShared.Plugin;
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.Plugin;
using System.Diagnostics;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class PluginTrigger : IComparable<PluginTrigger>
    {
        public Action<MockupServiceProviderAndFactory> PluginExecute { get; }

        internal string EntityName { get; }
        internal EventOperation Operation { get; }
        internal ExecutionStage Stage { get; }
        internal ExecutionMode Mode { get; }
        internal int Order { get; }
        internal Guid? ImpersonatingUserId { get; }

        readonly Dictionary<string, EntityMetadata> Metadata;
        readonly HashSet<string> Attributes;
        readonly IEnumerable<IImageSpecification> Images;

        private string DebuggerDisplay => $"{Stage} {Operation} {EntityName} [{string.Join(", ", Attributes)}]";

        public PluginTrigger(EventOperation operation, ExecutionStage stage,
                Action<MockupServiceProviderAndFactory> pluginExecute, IPluginStepConfig pluginRegistration, Dictionary<string, EntityMetadata> metadata)
        {
            PluginExecute = pluginExecute;
            EntityName = pluginRegistration.EntityLogicalName;
            Operation = operation;
            Stage = stage;
            Mode = pluginRegistration.ExecutionMode;
            Order = pluginRegistration.ExecutionOrder;
            Images = pluginRegistration.ImageSpecifications;
            Metadata = metadata;
            ImpersonatingUserId = pluginRegistration.ImpersonatingUserId;

            var attrs = pluginRegistration.FilteredAttributes ?? "";
            Attributes = string.IsNullOrWhiteSpace(attrs) ? new HashSet<string>() : new HashSet<string>(attrs.Split(','));
        }

        public ExecutionMode GetExecutionMode()
        {
            return Mode;
        }

        public int GetExecutionOrder()
        {
            return Order;
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
                return new PluginExecutionProvider(PluginExecute, new MockupServiceProviderAndFactory(core, thisPluginContext, core.TracingServiceFactory));
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
                    PluginExecute(provider);
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
            if (EntityName != "" && (Operation == EventOperation.Associate || Operation == EventOperation.Disassociate))
            {
                throw new MockupException(
                    $"An {Operation} plugin step was registered for a specific entity, which can only be registered on AnyEntity");
            }
        }

        private Entity AddPostImageAttributesToEntity(Entity entity, Entity preImage, Entity postImage)
        {
            if (Operation == EventOperation.Update && Stage == ExecutionStage.PostOperation)
            {
                var shadowAddedAttributes = postImage.Attributes.Where(a => !preImage.Attributes.ContainsKey(a.Key) && !entity.Attributes.ContainsKey(a.Key));
                entity = entity.CloneEntity();
                entity.Attributes.AddRange(shadowAddedAttributes);
            }
            return entity;
        }

        private bool FilteredAttributesMatches(Entity entity)
        {
            if (Operation != EventOperation.Update || Attributes.Count == 0)
            {
                return true;
            }

            bool foundAttr = false;
            foreach (var attr in entity.Attributes)
            {
                if (Attributes.Contains(attr.Key))
                {
                    foundAttr = true;
                    break;
                }
            }
            return foundAttr;
        }

        private bool VerifyPluginTrigger(Entity entity, string logicalName, Guid guid, Entity preImage, Entity postImage, PluginContext pluginContext)
        {
            if (EntityName != "" && EntityName != logicalName) return false;

            if (entity != null && Metadata.GetMetadata(logicalName)?.PrimaryIdAttribute != null)
            {
                entity[Metadata.GetMetadata(logicalName).PrimaryIdAttribute] = guid;
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
            thisPluginContext.Mode = (int)this.Mode;
            thisPluginContext.Stage = (int)this.Stage;
            if (thisPluginContext.PrimaryEntityId == Guid.Empty)
            {
                thisPluginContext.PrimaryEntityId = guid;
            }
            thisPluginContext.PrimaryEntityName = logicalName;
            if (ImpersonatingUserId is Guid impersonatingUserId && impersonatingUserId != Guid.Empty)
            {
                thisPluginContext.UserId = impersonatingUserId;
            }

            foreach (var image in this.Images)
            {
                var imageType = image.ImageType;
                var cols = !string.IsNullOrEmpty(image.Attributes) ? new ColumnSet(image.Attributes.Split(',')) : new ColumnSet(true);
                if (postImage != null && Stage == ExecutionStage.PostOperation && (imageType == ImageType.PostImage || imageType == ImageType.Both))
                {
                    thisPluginContext.PostEntityImages.Add(image.ImageName, postImage.CloneEntity(Metadata.GetMetadata(postImage.LogicalName), cols));
                }
                if (preImage != null && imageType == ImageType.PreImage || imageType == ImageType.Both)
                {
                    thisPluginContext.PreEntityImages.Add(image.ImageName, preImage.CloneEntity(Metadata.GetMetadata(preImage.LogicalName), cols));
                }
            }
            return thisPluginContext;
        }

        public int CompareTo(PluginTrigger other)
        {
            return Order.CompareTo(other.Order);
        }
    }
}
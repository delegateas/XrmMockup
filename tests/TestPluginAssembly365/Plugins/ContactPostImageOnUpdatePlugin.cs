using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;

namespace DG.Some.Namespace
{
    public class ContactPostImageOnUpdatePlugin : Plugin
    {
        public ContactPostImageOnUpdatePlugin()
        {
            RegisterStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .AddFilteredAttributes(c => c.Description)
                .WithPostImage();
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException(nameof(localContext));
            }

            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            if (target == null)
            {
                throw new InvalidPluginExecutionException("Target is null.");
            }

            var postImages = localContext.PluginExecutionContext.PostEntityImages;

            var service = localContext.OrganizationService;
            service.Create(new Task
            {
                Subject = $"PostImagePlugin executed. HasPostImage={postImages.Count > 0}",
                RegardingObjectId = new EntityReference(Contact.EntityLogicalName, target.Id),
            });
        }
    }
}

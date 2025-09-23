﻿using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Linq;

namespace DG.Some.Namespace
{
    public class IncidentDeleteAllRelatedResolutionsOnClose : Plugin
    {
        public IncidentDeleteAllRelatedResolutionsOnClose() : base(typeof(IncidentDeleteAllRelatedResolutionsOnClose))
        {
            RegisterPluginStep<Incident>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteDeleteAllRelatedResolutionsOnClose)
                .AddFilteredAttributes(x => x.StateCode)
                .AddImage(ImageType.PreImage, x => x.StateCode, x => x.Title)
                .AddImage(ImageType.PostImage, x => x.StateCode)
                .SetExecutionOrder(10);
        }

        protected void ExecuteDeleteAllRelatedResolutionsOnClose(LocalPluginContext localContext)
        {
            var preImage = localContext.PluginExecutionContext.PreEntityImages["PreImage"].ToEntity<Incident>();
            if (preImage.Title != "TestRemovalOfResolutionsAfterClose") return;

            var postImage = localContext.PluginExecutionContext.PostEntityImages["PostImage"].ToEntity<Incident>();

            if (preImage.StateCode == IncidentState.Active && postImage.StateCode != IncidentState.Active)
            {
                using (var context = new Xrm(localContext.OrganizationService))
                {
                    context.IncidentResolutionSet
                        .Where(x => x.IncidentId.Id == localContext.PluginExecutionContext.PrimaryEntityId)
                        .ToList()
                        .ForEach(x => localContext.OrganizationAdminService.Delete(x.LogicalName, x.Id));
                }
            }
        }
    }
}

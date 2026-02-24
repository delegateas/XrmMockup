using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using System.Linq;

namespace DG.Some.Namespace
{
    public class IncidentDeleteAllRelatedResolutionsOnClose : Plugin
    {
        public IncidentDeleteAllRelatedResolutionsOnClose()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Incident>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteDeleteAllRelatedResolutionsOnClose)
                .AddFilteredAttributes(x => x.StateCode)
                .AddImage(ImageType.PreImage, x => x.StateCode, x => x.Title)
                .AddImage(ImageType.PostImage, x => x.StateCode)
                .SetExecutionOrder(10);
#pragma warning restore CS0618 // Type or member is obsolete
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

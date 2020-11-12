using Microsoft.Xrm.Sdk;

namespace IXrmMockupExtension
{
    public interface IMockUpExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizationService">The extensions entry into XrmMockup (Dynamics 365)</param>
        /// <param name="request">Request used to determine what kind of operation have just been executed.</param>
        /// <param name="currentEntity">Newest version of the entity - only with updated attributes</param>
        /// <param name="userRef">The executing user - used to generate OrgService to execute request against XrmMockup</param>
        void TriggerExtension(IOrganizationService organizationService,OrganizationRequest request, Entity currentEntity, EntityReference userRef);
    }
}
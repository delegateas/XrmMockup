using System;
using System.Collections.Generic;
using System.Text;

#if DATAVERSE_SERVICE_CLIENT
using Microsoft.PowerPlatform.Dataverse.Client;
#endif

namespace DG.Tools.XrmMockup
{
#if DATAVERSE_SERVICE_CLIENT
    public abstract partial class XrmMockupBase
    {
        /// <summary>
        /// Create an async organization service for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync CreateOrganizationServiceAsync(Guid userId)
        {
            return ServiceFactory.CreateOrganizationServiceAsync(userId);
        }

        /// <summary>
        /// Create an async organization service, with the given settings, for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync CreateOrganizationServiceAsync(Guid userId, MockupServiceSettings settings)
        {
            return ServiceFactory.CreateOrganizationServiceAsync(userId, settings);
        }

        /// <summary>
        /// Gets an async system administrator organization service
        /// </summary>
        /// <returns></returns>
        public IOrganizationServiceAsync GetAdminServiceAsync()
        {
            return ServiceFactory.CreateAdminOrganizationServiceAsync();
        }

        /// <summary>
        /// Gets an async system administrator organization service, with the given settings
        /// </summary>
        /// <param name="Settings"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync GetAdminServiceAsync(MockupServiceSettings Settings)
        {
            return ServiceFactory.CreateAdminOrganizationServiceAsync(Settings);
        }
    }
#endif
}

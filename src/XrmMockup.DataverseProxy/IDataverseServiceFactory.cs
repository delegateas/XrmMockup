using Microsoft.PowerPlatform.Dataverse.Client;

namespace XrmMockup.DataverseProxy;

/// <summary>
/// Factory interface for creating IOrganizationServiceAsync2 instances.
/// Enables dependency injection and testing of ProxyServer.
/// </summary>
public interface IDataverseServiceFactory
{
    /// <summary>
    /// Creates or returns an IOrganizationServiceAsync2 instance for Dataverse operations.
    /// </summary>
    IOrganizationServiceAsync2 CreateService();
}

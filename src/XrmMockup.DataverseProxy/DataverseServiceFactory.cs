using Microsoft.PowerPlatform.Dataverse.Client;

namespace XrmMockup.DataverseProxy;

/// <summary>
/// Factory that provides IOrganizationServiceAsync2 from a ServiceClient.
/// </summary>
public class DataverseServiceFactory : IDataverseServiceFactory
{
    private readonly ServiceClient _serviceClient;

    public DataverseServiceFactory(ServiceClient serviceClient)
    {
        _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
    }

    public IOrganizationServiceAsync2 CreateService() => _serviceClient;
}

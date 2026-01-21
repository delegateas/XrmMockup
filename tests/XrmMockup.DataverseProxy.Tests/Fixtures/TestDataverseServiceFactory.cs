using Microsoft.PowerPlatform.Dataverse.Client;
using XrmMockup.DataverseProxy;

namespace XrmMockup.DataverseProxy.Tests.Fixtures;

/// <summary>
/// Test factory that wraps a mock IOrganizationServiceAsync2.
/// </summary>
public class TestDataverseServiceFactory : IDataverseServiceFactory
{
    private readonly IOrganizationServiceAsync2 _service;

    public TestDataverseServiceFactory(IOrganizationServiceAsync2 service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IOrganizationServiceAsync2 CreateService() => _service;
}

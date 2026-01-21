using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerPlatform.Dataverse.Client;
using XrmMockup.DataverseProxy.Contracts;

namespace XrmMockup.DataverseProxy;

/// <summary>
/// Factory that creates MockDataService instances from a JSON data file.
/// Used for testing proxy communication without connecting to Dataverse.
/// </summary>
internal class MockDataServiceFactory : IDataverseServiceFactory
{
    private readonly MockDataService _service;

    public MockDataServiceFactory(string dataFilePath)
    {
        var json = File.ReadAllText(dataFilePath);
        var data = JsonSerializer.Deserialize<MockDataFile>(json);

        var entities = data?.Entities?
            .Select(EntitySerializationHelper.DeserializeEntity)
            .ToList() ?? [];

        _service = new MockDataService(entities);
    }

    public IOrganizationServiceAsync2 CreateService() => _service;
}

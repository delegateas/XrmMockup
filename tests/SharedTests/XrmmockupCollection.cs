using Xunit;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// Ensures that XrmMockupFixture is only initialized once for all tests in the collection.
    /// Must be defined in the same assembly as the tests.
    /// </summary>
    [CollectionDefinition("Xrm Collection")]
    public class XrmMockupCollection : ICollectionFixture<XrmMockupFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
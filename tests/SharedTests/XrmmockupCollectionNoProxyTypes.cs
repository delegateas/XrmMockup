using Xunit;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// DEPRECATED: Collection no longer needed as each test gets its own instance
    /// Legacy collection definition kept for backward compatibility but not used
    /// Tests now run in parallel with individual XrmMockup365 instances
    /// </summary>
    [CollectionDefinition("Xrm Collection No Proxy Types")]
    public class XrmMockupCollectionNoProxyTypes : ICollectionFixture<XrmMockupFixtureNoProxyTypes>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
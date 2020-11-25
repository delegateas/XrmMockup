using System;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrieveVersion : UnitTestBase
    {
        public TestRetrieveVersion(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestRetrieveVersionAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var version = (RetrieveVersionResponse)orgAdminUIService.Execute(new RetrieveVersionRequest());
#if XRM_MOCKUP_TEST_2011
                Assert.Equal("5", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_2013
                Assert.Equal("6", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_2015
                Assert.Equal("7", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_2016
                Assert.Equal("8", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_365
                Assert.True(8 <= Int32.Parse(version.Version.Substring(0, 1)));
#endif
            }
        }
    }

}
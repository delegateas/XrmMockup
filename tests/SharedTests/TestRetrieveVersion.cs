using System;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestRetrieveVersion : UnitTestBase
    {
        [TestMethod]
        public void TestRetrieveVersionAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var version = (RetrieveVersionResponse) orgAdminUIService.Execute(new RetrieveVersionRequest());
#if XRM_MOCKUP_TEST_2011
                Assert.AreEqual("5", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_2013
                Assert.AreEqual("6", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_2015
                Assert.AreEqual("7", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_2016
                Assert.AreEqual("8", version.Version.Substring(0, 1));
#elif XRM_MOCKUP_TEST_365
                Assert.IsTrue(8 <= Int32.Parse(version.Version.Substring(0, 1)));
#endif
            }
        }
    }

}
using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestOrganizationService : UnitTestBase
    {
        [TestMethod]
        public void TestOrgSvcWithNonExistentUser()
        {
            try
            {
                crm.CreateOrganizationService(Guid.NewGuid());
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }
    }
}

using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmMockupTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestOrganizationService : UnitTestBase {
        [TestMethod]
        public void TestOrgSvcWithNonExistentUser() {
            try {
                crm.CreateOrganizationService(Guid.NewGuid());
                Assert.Fail();
            } catch (Exception e) {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }
    }
}

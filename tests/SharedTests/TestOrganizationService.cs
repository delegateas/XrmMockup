using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmMockupTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedTests
{
    [TestClass]
    public class TestOrganizationService : UnitTestBase
    {
        [TestMethod]
        public void TestOrgSvcWithNonExistentUser()
        {
            var origAdminUser = crm.AdminUser.Id;
            crm.ResetEnvironment();
            var newAdminUser = crm.AdminUser.Id;

            var origService = crm.CreateOrganizationService(origAdminUser);
            using (var context = new Xrm(origService))
            {
                //Currently throws an exception. Should return null
                var acc = context.AccountSet.FirstOrDefault(); 
                Assert.IsNull(acc);
            }
        }
    }
}

using System;
using DG.XrmContext;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestDelete : UnitTestBase
    {
        [TestMethod]
        public void DeleteTest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var guid = orgAdminUIService.Create(new Contact());

                var firstRetrieve = orgAdminUIService.Retrieve<Contact>(guid, null);
                Assert.IsNotNull(firstRetrieve);

                orgAdminUIService.Delete(Contact.EntityLogicalName, guid);

                try
                {
                    orgAdminUIService.Retrieve<Contact>(guid, null);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

            }
        }

        [TestMethod]
        public void TestDeleteNonExistingEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                try
                {
                    orgAdminUIService.Delete(Contact.EntityLogicalName, Guid.NewGuid());
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

            }
        }
    }

}

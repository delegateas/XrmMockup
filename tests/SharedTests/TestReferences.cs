using System;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestReferences : UnitTestBase
    {
        [TestMethod]
        public void TestCreateCircularReferenceSelf()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id = Guid.NewGuid();
                var acc = new Account(id)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id)
                };
                try
                {
                    orgAdminUIService.Create(acc);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
            }
        }


        [TestMethod]
        public void TestUpdateCircularReference()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id = Guid.NewGuid();
                var acc = new Account(id);
                orgAdminUIService.Create(acc);

                acc.ParentAccountId = new EntityReference(Account.EntityLogicalName, id);
                try
                {
                    orgAdminUIService.Update(acc);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

                acc.ParentAccountId = null;
                orgAdminUIService.Update(acc);
            }
        }
    }

}

using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmContext;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestDelete : UnitTestBase {

        [TestMethod]
        public void DeleteTest() {
            using (var context = new Xrm(orgAdminUIService)) {
                
                var guid = orgAdminUIService.Create(new Contact());

                var firstRetrieve = orgAdminUIService.Retrieve<Contact>(guid, null);
                Assert.IsNotNull(firstRetrieve);

                orgAdminUIService.Delete(Contact.EntityLogicalName, guid);

                try {
                    orgAdminUIService.Retrieve<Contact>(guid, null);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }
                
            }
        }

        [TestMethod]
        public void TestDeleteNonExistingEntity() {
            using (var context = new Xrm(orgAdminUIService)) {
                try {
                    orgAdminUIService.Delete(Contact.EntityLogicalName, Guid.NewGuid());
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(FaultException));
                }

            }
        }
    }

}

using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestSettings : UnitTestBase {
        [TestMethod]
        public void TestNoExceptionRequest() {
            using (var context = new Xrm(orgAdminUIService)) {

                var req = new OrganizationRequest("WrongRequestThatFails");
                try {
                    orgAdminUIService.Execute(req);
                    Assert.Fail();
                } catch (Exception e) {
                    Assert.IsInstanceOfType(e, typeof(NotImplementedException));
                }

                req = new OrganizationRequest("TestWrongRequest");
                orgAdminUIService.Execute(req);
            }
        }
    }

}

using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace DG.XrmMockupTest {

    [TestClass]
    public class TestActivities : UnitTestBase {

        [TestMethod]
        public void TestCreationOfActivites() {
            using (var context = new Xrm(orgAdminUIService)) {
                
            }
        }
    }
}
#if XRM_MOCKUP_TEST_2016 || XRM_MOCKUP_TEST_365
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
    public class TestAction : UnitTestBase {

        [TestMethod]
        public void TestActionExecution() {
            using (var context = new Xrm(orgAdminUIService)) {
                var someString = "A some string";
                var entity = new Contact();
                entity.Id = orgAdminUIService.Create(entity);
                var req = new OrganizationRequest("ActionTest");
                req["SomeString"] = someString;
                req["Target"] = entity.ToEntityReference();
                var resp = orgAdminUIService.Execute(req);
                var leadRef = resp["CreatedEntity"] as EntityReference;
                var lead = orgAdminUIService.Retrieve(leadRef.LogicalName, leadRef.Id, new ColumnSet(true)) as Lead;
                Assert.AreEqual(someString, lead.LastName);
                Assert.AreEqual("From Action", lead.Subject);
            }
        }

        [TestMethod]
        public void TestActionParts() {
            using (var context = new Xrm(orgAdminUIService)) {
                var stringInput = "A string";
                var datetimeInput = DateTime.Now;
                var boolInput = true;
                var decimalInput = 12.3m;
                var floatInput = 412.2f;
                var intInput = 12;
                var moneyInput = new Money(123.7m);
                var pickListInput = new OptionSetValue(3);

                var entity = new Contact();
                entity.Id = orgAdminUIService.Create(entity);

                var req = new OrganizationRequest("Full action");
                req["StringInput"] = stringInput;
                req["Target"] = entity.ToEntityReference();
                var resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                var output = resp["Output"] as string;
                Assert.AreEqual(stringInput, output);

                req = new OrganizationRequest("Full action");
                req["DateTimeInput"] = datetimeInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
                Assert.AreEqual(datetimeInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["BoolInput"] = boolInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
                Assert.AreEqual(boolInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["DecimalInput"] = decimalInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
                Assert.AreEqual(decimalInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["FloatInput"] = floatInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
                Assert.AreEqual(floatInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["IntegerInput"] = intInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
                Assert.AreEqual(intInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["PicklistInput"] = pickListInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.IsTrue(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
                Assert.AreEqual(pickListInput.ToString(), output);

            }
        }
    }

}
#endif
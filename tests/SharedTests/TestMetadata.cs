using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Metadata;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestMetadata : UnitTestBase {

        [TestMethod]
        public void TestRetrieveOptionSet() {
            using (var context = new Xrm(orgAdminUIService)) {
                var optionRetrieved = orgAdminUIService.Execute(new RetrieveOptionSetRequest() { Name = "workflow_stage" }) as RetrieveOptionSetResponse;
                Assert.IsTrue(optionRetrieved.OptionSetMetadata.Name == "workflow_stage");

            }
        }

        [TestMethod]
        public void TestRetrieveAllOptionSets() {
            using (var context = new Xrm(orgAdminUIService)) {
                var optionsRetrieved = orgAdminUIService.Execute(new RetrieveAllOptionSetsRequest()) as RetrieveAllOptionSetsResponse;
                Assert.IsTrue(optionsRetrieved.OptionSetMetadata.Any(x => x.Name == "workflow_stage"));

            }
        }

        [TestMethod]
        public void TestSetttingAttributes() {
            using (var context = new Xrm(orgAdminUIService)) {
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc.Attributes["name"] = "Jon";
                orgAdminUIService.Update(acc);

                try {
                    acc.Attributes["illegalName"] = 1;
                    orgAdminUIService.Update(acc);
                    Assert.Fail("FaultException should have been thrown");
                } catch (FaultException) {
                } catch (Exception e) {
                    Assert.Fail(
                         string.Format("Unexpected exception of type {0} caught: {1}",
                                        e.GetType(), e.Message)
                    );
                }
            }
        }

        [TestMethod]
        public void TestCRURestrictions() {
            using (var context = new Xrm(orgAdminUIService)) {
                var acc = new Account();
                acc.Attributes.Add("opendeals_state", 22);
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("opendeals_state")) as Account;
                Assert.AreNotEqual(retrieved.OpenDeals_State, 22);

                orgAdminUIService.Update(acc);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("opendeals_state")) as Account;
                Assert.AreNotEqual(retrieved.OpenDeals_State, 22);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("isprivate")) as Account;
                Assert.IsFalse(retrieved.Attributes.ContainsKey("isprivate"));

            }
        }


        [TestMethod]
        public void RetrieveEntityMetadata() {
            var req = new RetrieveEntityRequest() {
                LogicalName = Account.EntityLogicalName
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNotNull(resp);
            Assert.IsNotNull(resp.EntityMetadata);
            Assert.AreEqual(req.LogicalName, resp.EntityMetadata.LogicalName);
        }

    }

}

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
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestOpportunity : UnitTestBase {

        [TestMethod]
        public void TestWinOpportunity() {
            using (var context = new Xrm(orgAdminUIService)) {
                var opportunity = new Opportunity();
                opportunity.Id = orgAdminUIService.Create(opportunity);

                var winReq = new WinOpportunityRequest();
                var opclose = new OpportunityClose() {
                    ActualRevenue = 1000m,
                    ActualEnd = DateTime.Now,
                    StateCode = OpportunityCloseState.Completed,
                    StatusCode = OpportunityClose_StatusCode.Completed,
                    OpportunityId = opportunity.ToEntityReference()
                };
                winReq.OpportunityClose = opclose;
                winReq.Status = new OptionSetValue((int)Opportunity_StatusCode.Won);

                orgAdminUIService.Execute(winReq);

                var retrieved = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunity.Id, new ColumnSet(true)) as Opportunity;
                Assert.AreEqual(OpportunityState.Won, retrieved.StateCode);
                Assert.AreEqual(Opportunity_StatusCode.Won, retrieved.StatusCode);
                Assert.IsTrue(crm.ContainsEntity(opclose));
                Assert.AreEqual("SetFromWinLose", retrieved.Description);
            }
        }

        [TestMethod]
        public void TestLoseOpportunity() {
            using (var context = new Xrm(orgAdminUIService)) {
                var opportunity = new Opportunity();
                opportunity.Id = orgAdminUIService.Create(opportunity);

                var loseReq = new LoseOpportunityRequest();
                var opclose = new OpportunityClose() {
                    ActualRevenue = 1000m,
                    ActualEnd = DateTime.Now,
                    StatusCode = OpportunityClose_StatusCode.Canceled,
                    OpportunityId = opportunity.ToEntityReference()
                };
                loseReq.OpportunityClose = opclose;
                loseReq.Status = new OptionSetValue((int)Opportunity_StatusCode.Canceled);

                orgAdminUIService.Execute(loseReq);

                var retrieved = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunity.Id, new ColumnSet(true)) as Opportunity;
                Assert.AreEqual(OpportunityState.Lost, retrieved.StateCode);
                Assert.AreEqual(Opportunity_StatusCode.Canceled, retrieved.StatusCode);
                Assert.IsTrue(crm.ContainsEntity(opclose));
                Assert.AreEqual("SetFromWinLose", retrieved.Description);
            }
        }
    }
}

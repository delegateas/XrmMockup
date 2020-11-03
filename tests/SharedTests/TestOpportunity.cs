using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestOpportunity : UnitTestBase
    {
        [TestMethod]
        public void TestWinOpportunity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var opportunity = new Opportunity();
                opportunity.Id = orgAdminUIService.Create(opportunity);

                var winReq = new WinOpportunityRequest();
                var opclose = new OpportunityClose()
                {
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

                var checkOpClose = orgAdminService.Retrieve(opclose.LogicalName, opclose.Id, new ColumnSet(true));


                Assert.AreEqual("SetFromWinLose", retrieved.Description);
            }
        }

        [TestMethod]
        public void TestLoseOpportunity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var opportunity = new Opportunity();
                opportunity.Id = orgAdminUIService.Create(opportunity);

                var loseReq = new LoseOpportunityRequest();
                var opclose = new OpportunityClose()
                {
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
                var checkOpClose = orgAdminService.Retrieve(opclose.LogicalName, opclose.Id, new ColumnSet(true));
                Assert.AreEqual("SetFromWinLose", retrieved.Description);
            }
        }
    }
}

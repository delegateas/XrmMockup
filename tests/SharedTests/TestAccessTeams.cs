#if !XRM_MOCKUP_TEST_2011
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Crm.Sdk.Messages;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestAccessTeams : UnitTestBase
    {
        
        [TestMethod]
        public void TestCreateSimple()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            //check that user 1 cannot see contact 2
            try
            {
                var checkContact = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have permission"))
                {
                    throw;
                }
            }

            var req = new AddUserToRecordTeamRequest();
            req.Record = contact2.ToEntityReference();
            req.SystemUserId = testUser1.Id;

            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, "TestReadContact");
            var tt = orgAdminService.RetrieveMultiple(q);

            req.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(req);

            //that user 1 can now see contact 2
            var checkContact2 = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));

            var removereq = new RemoveUserFromRecordTeamRequest();
            removereq.Record = contact2.ToEntityReference();
            removereq.SystemUserId = testUser1.Id;
            removereq.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(removereq);

            //check that user 1 cannot see contact 2
            try
            {
                var checkContact = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have permission"))
                {
                    throw;
                }
            }


        }


    }
}
#endif

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
                FirstName = "John"
            };
            contact.Id = orgAdminService.Create(contact);

            var req = new AddUserToRecordTeamRequest();
            req.Record = contact.ToEntityReference();
            req.SystemUserId = salesUser.Id;
            req.TeamTemplateId = contactWriteAccessTeamTemplate.Id;

            orgAdminService.Execute(req);

            
        }

        
    }
}
#endif

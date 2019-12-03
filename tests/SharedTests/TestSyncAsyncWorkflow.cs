#if !(XRM_MOCKUP_TEST_2011)
using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace.Test;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.IO;

namespace DG.XrmMockupTest
{

    [TestClass]
    public class TestSyncAsyncWorkflow : UnitTestBase
    {
        Account account;
        string oldAccountName;
        string newAccountName;


        /* EmailAddress1 update triggers Async workflow and a Sync workflow, that triggers another sync workflow. 
         * Should be applied in order Sync1 Sync2 Async
         */
        [TestMethod]
        public void SyncAndAsyncWorkflowSucceedsWhenSyncAppliesFirst()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync1.xml"));
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateASync.xml"));
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync2.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync1" + "Sync2" + "ASync";

                account = new Account()
                {
                    Name = oldAccountName,
                };
                var accountId = orgAdminService.Create(account);

                var accountUpd = new Account(accountId)
                {
                    EMailAddress1 = "trigger@valid.dk"
                };

                orgAdminService.Update(accountUpd);

                var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

                Assert.AreEqual(newAccountName, retrievedAccount.Name);
            }
        }
        /* EmailAddress1 update triggers Async workflow and a Sync workflow, that triggers another sync workflow. 
         * Only Sync3 should apply */
        [TestMethod]
        public void Sync3AndSync1TriggerSync2SucceedsWhenSync3AppliesFirst()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync1.xml"));
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync2.xml"));
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync3.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync3";

                account = new Account()
                {
                    Name = oldAccountName,
                };
                var accountId = orgAdminService.Create(account);

                var accountUpd = new Account(accountId)
                {
                    EMailAddress1 = "trigger@valid.dk"
                };

                orgAdminService.Update(accountUpd);

                var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

                Assert.AreEqual(newAccountName, retrievedAccount.Name);
            }
        }
    }
}
#endif
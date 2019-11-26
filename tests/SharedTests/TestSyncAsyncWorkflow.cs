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

        [TestMethod]
        public void Sync3Sync1TriggersSync2SucceedsWhenOnlySync3Applies()
        {
            /* Create Sync1 and Sync3 plugins + workflows that trigger on account email address 1 update.
             * They append their type to account name and sync1 edits emailaddress 2
             * which triggers Sync2, that also appends type to account name*/

            throw new NotImplementedException();

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
#endif
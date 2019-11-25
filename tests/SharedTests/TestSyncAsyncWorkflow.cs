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
            crm.DisableRegisteredPlugins(true);
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync1.xml"));
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateSync2.xml"));
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "TestNameUpdateASync.xml"));
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
    }
}
﻿#if !(XRM_MOCKUP_TEST_2011) && !(XRM_MOCKUP_TEST_2013) && !(XRM_MOCKUP_TEST_2015) && !(XRM_MOCKUP_TEST_2016)
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestSyncAsyncWorkflow : UnitTestBase
    {
        Account account;
        string oldAccountName;
        string newAccountName;

        /*Tests concerning execution of synchronous and asynchronous workflows. All workflows can be found in the folder "TestSyncAsyncWorkflows"*/
        [TestMethod]
        public void Test1Async2Sync1TriggerSync3SuccedsWhenAllApplies()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowSync1.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowSync3.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowASync2.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync1" + "Sync3" + "ASync2";

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
        public void Test2Sync1Sync2SucceedsWhenOnlySync2Applies()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.DisableRegisteredPlugins(true);

                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync1.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync2.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync2";

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
        public void Test3Async1Async2SucceedsWhenBothApply()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test3", "Test3WorkflowASync1.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test3", "Test3WorkflowASync2.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "ASync1" + "ASync2";

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
        public void Test4Sync1Async2SucceedsWhenBothApplies()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test4", "Test4WorkflowAsync2.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test4", "Test4WorkflowSync1.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync1" + "ASync2";

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
        public void Test5Sync3Sync1TriggersAsync2SucceedsWhenSync1AndAsync2Applies()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test5", "Test5WorkflowAsync2.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test5", "Test5WorkflowSync1.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test5", "Test5WorkflowSync3.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync3" + "Async2";

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
        public void Test6Async3Sync1TriggersAsync2SucceedsWhenAllApplies()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test6", "Test6WorkflowAsync2.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test6", "Test6WorkflowSync1.xml"));
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test6", "Test6WorkflowASync3.xml"));

                oldAccountName = "Test";
                newAccountName = oldAccountName + "Sync1" + "ASync2" + "ASync3";

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
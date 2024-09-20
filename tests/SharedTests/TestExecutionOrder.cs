using DG.Some.Namespace.Test;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.IO;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestExecutionOrder : UnitTestBase
    {
        public TestExecutionOrder(XrmMockupFixture fixture) : base(fixture) { crm.DisableRegisteredPlugins(true); }

        [Fact]
        public void TestWorkflowBeforePluginSync()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(Test1Plugin1));                             // Appends: Sync1
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync2.xml"));    // Appends: Sync2

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "Sync1", retrievedAccount.Name);
        }

        [Fact]
        public void TestPluginBeforeWorkflowAsync()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(Test2Plugin1));                             // Appends: ASync1
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowASync2.xml"));   // Appends: ASync2

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "ASync1" + "ASync2", retrievedAccount.Name);
        }

        [Fact]
        public void TestPluginBeforeWorkflowSync()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(TestPlugin0));                              // Appends: Sync0
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync2.xml"));    // Appends: Sync2

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "Sync2", retrievedAccount.Name);
        }

        //[Fact]
        //public void TestGeneralEexecutionOrder()
        //{
        //    crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
        //        typeof(Test1Plugin1),   // Sync
        //        typeof(Test2Plugin1));  // Async

        //    crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync2.xml"));    // Sync
        //    crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowASync2.xml"));   // Async

        //    var account = new Account()
        //    {
        //        Name = "Test",
        //    };
        //    account.Id = orgAdminService.Create(account);

        //    account.EMailAddress1 = "trigger";
        //    orgAdminService.Update(account);

        //    var expectedName = account.Name + "Sync2" + "Sync1" + "ASync1" + "ASync2";

        //    var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

        //    Assert.Equal(expectedName, retrievedAccount.Name);
        //}
    }
}

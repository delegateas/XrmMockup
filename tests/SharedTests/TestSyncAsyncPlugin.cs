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

namespace DG.XrmMockupTest
{

    [TestClass]
    public class TestSyncAsyncPlugin : UnitTestBase
    {
        /*-------------------------------- Order of plugin declaration --------------------------
         * Registration order of plugins should not interfere with execution. Sync should
         * always execute before Async unless execution order is given.
         */
        [TestMethod]
        public void TestSyncAndAsyncPluginFailsWhenAsyncAppliesFirst()
        {
            //OrderOfPluginDeclaration -> Register ASync first, then Sync 
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(ASyncNameUpdate),
                typeof(SyncPostOperationNameUpdate));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + ", Sync" + ", ASync";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            var personelId = orgAdminService.Create(personel);

            var personelUpd = new Contact(personelId)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);
            
            Assert.AreEqual(newFirstName, retrievedPersonel.FirstName);
        }

        [TestMethod]
        public void TestSyncAndAsyncPluginSucceedsWhenSyncAppliesFirst()
        {
            //OrderOfPluginDeclaration -> Register ASync first, then Sync 
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(SyncPostOperationNameUpdate),
                typeof(ASyncNameUpdate));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + ", Sync" + ", ASync";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            var personelId = orgAdminService.Create(personel);

            var personelUpd = new Contact(personelId)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.AreEqual(newFirstName, retrievedPersonel.FirstName);
        }

        /* ---------------------------------- Execution Order --------------------------------
         * Plugin with lowest executionOrder should execute first. If no order given, Sync
         * executes before Async. 
         */

        [TestMethod]
        public void TestTwoSyncPluginsLowestExecutionOrderExecutesFirst()
        {
            //OrderOfPluginDeclaration -> Register ASync first, then Sync 
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Sync1WithExecutionOrder),
                typeof(Sync2WithExecutionOrder));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + ", Sync1" + ", Sync2";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            var personelId = orgAdminService.Create(personel);

            var personelUpd = new Contact(personelId)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.AreEqual(newFirstName, retrievedPersonel.FirstName);
        }

        [TestMethod]
        public void TestSyncPluginCallsSyncAndAsyncPluginNoExecutionOrder()
        {
            //Sync plugin calls another Sync and Async plugin. Sync executes first.
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Sync1PostOperation),
                typeof(Sync2PostOperation),
                typeof(AsyncPostOperation));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + ", Sync" + ", Sync" + ", ASync";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            var personelId = orgAdminService.Create(personel);

            var personelUpd = new Contact(personelId)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.AreEqual(newFirstName, retrievedPersonel.FirstName);
        }

        [TestMethod]
        public void TestSyncPluginCallsSyncAndAsyncPluginWithExecutionOrder()
        {
            //Sync plugin calls another Sync2 plugin with EO(3) and Async plugin with EO(2). Succeeds when Async executes before Sync2 

        }
    }
}
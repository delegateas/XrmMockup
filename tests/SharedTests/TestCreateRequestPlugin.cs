using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup;

namespace SharedTests
{
    [TestClass]
    public class TestCreateRequestPlugin
    {
        private IOrganizationService service;

        [TestInitialize]
        public void TestInitialize()
        {
            service = MockupServiceFactory.CreateOrganizationService();
        }

        [TestMethod]
        public void TestCreateRequestPluginExecution()
        {
            var entity = new Entity("account");
            entity["firstname"] = "John";
            entity["lastname"] = "Doe";

            var createRequest = new CreateRequest { Target = entity };
            service.Execute(createRequest);

            var createdEntity = service.Retrieve("account", entity.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            Assert.AreEqual("Bob", createdEntity["firstname"]);
            Assert.AreEqual("Saget", createdEntity["lastname"]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmMockupTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestCWAAccountOptional : UnitTestBase
    {

        [TestMethod]
        public void TestWorkFlowWithEmptyOptionalParameter()
        {
            crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "CWAAccountoptional.xml"));

            var account = new Account();

            account.Id = orgAdminService.Create(account);
        }

        [TestMethod]
        public void TestWorkFlowWithFilledOptionalParameter()
        {
            crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "CWAAccountoptionalWithDateValue.xml"));

            var account = new Account();

            account.Id = orgAdminService.Create(account);
        }
    }
}

using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public class TestSecurity : UnitTestBase
    {
        public TestSecurity(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestBasicSecurity()
        {
            var child = new Entity("mock_child");
            child.Id = orgAdminService.Create(child);

            


        }

    }

}

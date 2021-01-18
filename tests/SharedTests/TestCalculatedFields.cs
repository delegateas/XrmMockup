using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestCalculatedFields : UnitTestBase
    {
        public TestCalculatedFields(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCalculatedField()
        {
            var calc = new Entity("mock_calculatedfield");
            calc["mock_total"] = 22;
            calc["mock_signed"] = 10;
            var calcId = orgAdminService.Create(calc);

            var checkCalc = orgAdminService.Retrieve("mock_calculatedfield", calcId, new ColumnSet(true));
            Assert.Equal(12, checkCalc.GetAttributeValue<int>("mock_unsigned"));

        }
    }
}

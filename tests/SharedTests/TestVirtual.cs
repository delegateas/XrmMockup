using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace DG.XrmMockupTest
{
    public class TestVirtual : UnitTestBase
    {
        public TestVirtual(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void VirtualTest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var bus = new dg_bus
                {
                    dg_name = "HelloBus"
                };
                var busId = orgAdminService.Create(bus);

                orgAdminService.Update(new dg_bus
                {
                    dg_busId = busId,
                    dg_dokumenttyper = new List<dg_dokumenttyper>() { dg_dokumenttyper.Doc, dg_dokumenttyper.PDF }
                });

                var retrieved = orgAdminService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                var dsdsds = context.dg_busSet.ToList();
                Assert.True(retrieved.dg_dokumenttyper.Any());
                Assert.Equal(new List<dg_dokumenttyper>() { dg_dokumenttyper.Doc, dg_dokumenttyper.PDF }, retrieved.dg_dokumenttyper);
            }
        }
    }
}

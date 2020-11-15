#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestMoney : UnitTestBase
    {
        [TestMethod]
        public void TestCalculatedIsSet()
        {

            var bus = new dg_bus
            {
                dg_name = "Buu"
            };
            bus.Id = orgAdminUIService.Create(bus);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.IsNull(retrieved.dg_Udregnet);
            }

            bus.dg_name = "Woop";
            orgAdminUIService.Update(bus);
            using (var context = new Xrm(orgAdminUIService))
            {
                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(0,retrieved.dg_Udregnet);
                Assert.IsNull(bus.dg_AllConditions);
            }

            bus.dg_Ticketprice = 30;
            bus.dg_EtHelTal = 5;
            bus.dg_Udkoerselsdato = DateTime.Now;
            orgAdminUIService.Update(bus);
            using (var context = new Xrm(orgAdminUIService))
            {
                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(bus.dg_Ticketprice * 20, retrieved.dg_Udregnet);
                Assert.AreEqual(bus.dg_EtHelTal - 2, retrieved.dg_WholenumberUdregnet);
                Assert.AreEqual(0,(bus.dg_Udkoerselsdato.Value.AddDays(2) - retrieved.dg_DateTimeUdregnet).Value.Seconds);
                Assert.AreEqual(bus.dg_name.Substring(2), retrieved.dg_TrimLeft);
                Assert.IsNotNull(retrieved.dg_AllConditions);
            }
        }

        [TestMethod]
        public void TestRollUp()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var childlessBus = new dg_bus() { dg_name = "Buu" };
                childlessBus.Id = orgAdminUIService.Create(childlessBus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, childlessBus.Id, new ColumnSet(true)) as dg_bus;
                Assert.IsNull(retrieved.dg_Totalallowance);

                childlessBus.dg_name = "Woop";
                orgAdminUIService.Update(childlessBus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, childlessBus.Id, new ColumnSet(true)) as dg_bus;
                Assert.IsNull(retrieved.dg_Totalallowance);

                var bus = new dg_bus { dg_name = "Woop" };
                bus.Id = orgAdminUIService.Create(bus);

                orgAdminUIService.Create(
                    new dg_child()
                    {
                        dg_name = "Hans Jørgen",
                        dg_Allowance = 20,
                        dg_Skolebus = new EntityReference
                        {
                            Id = bus.Id,
                            LogicalName = dg_bus.EntityLogicalName
                        }
                    });

                orgAdminUIService.Create(
                    new dg_child()
                    {
                        dg_name = "Hans Gert",
                        dg_Allowance = 50,
                        dg_Skolebus = new EntityReference
                        {
                            Id = bus.Id,
                            LogicalName = dg_bus.EntityLogicalName
                        }
                    });

                var anotherCurrency = new TransactionCurrency()
                {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 2
                };
                anotherCurrency.Id = orgAdminUIService.Create(anotherCurrency);

                orgAdminUIService.Create(
                   new dg_child()
                   {
                       dg_name = "Børge Hansen",
                       dg_Allowance = 30,
                       dg_Skolebus = new EntityReference
                       {
                           Id = bus.Id,
                           LogicalName = dg_bus.EntityLogicalName
                       },
                       TransactionCurrencyId = anotherCurrency.ToEntityReference()
                   });

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.IsNull(retrieved.dg_Totalallowance);
                Assert.IsNull(retrieved.dg_MaxAllowance);
                Assert.IsNull(retrieved.dg_MinAllowance);
                Assert.IsNull(retrieved.dg_AvgAllowance);

                var req = new CalculateRollupFieldRequest()
                {
                    FieldName = "dg_totalallowance",
                    Target = bus.ToEntityReference()
                };
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(130, retrieved.dg_Totalallowance);
                Assert.IsNull(retrieved.dg_MaxAllowance);
                Assert.IsNull(retrieved.dg_MinAllowance);
                Assert.IsNull(retrieved.dg_AvgAllowance);

                req.FieldName = "dg_maxallowance";
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(130, retrieved.dg_Totalallowance);
                Assert.AreEqual(60, retrieved.dg_MaxAllowance);
                Assert.IsNull(retrieved.dg_MinAllowance);
                Assert.IsNull(retrieved.dg_AvgAllowance);

                req.FieldName = "dg_minallowance";
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(130, retrieved.dg_Totalallowance);
                Assert.AreEqual(60, retrieved.dg_MaxAllowance);
                Assert.AreEqual(20, retrieved.dg_MinAllowance);
                Assert.IsNull(retrieved.dg_AvgAllowance);

                req.FieldName = "dg_avgallowance";
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(130, retrieved.dg_Totalallowance.Value);
                Assert.AreEqual(60, retrieved.dg_MaxAllowance.Value);
                Assert.AreEqual(20, retrieved.dg_MinAllowance.Value);
                Assert.AreEqual(43.33m, retrieved.dg_AvgAllowance.Value);
            }
        }

        [TestMethod]
        public void TestPrecisionSource()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var currency = new TransactionCurrency()
                {
                    ExchangeRate = 1m,
                    CurrencyPrecision = 3
                };
                currency.Id = orgAdminUIService.Create(currency);

                var bus = new dg_bus()
                {
                    dg_name = "Woop",
                    dg_Ticketprice = 10.1234m,
                    TransactionCurrencyId = currency.ToEntityReference()
                };
                bus.Id = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value, currency.CurrencyPrecision.Value), retrieved.dg_Ticketprice.Value);
                Assert.AreEqual(Math.Round(Math.Round(bus.dg_Ticketprice.Value, currency.CurrencyPrecision.Value) * 20, 2), retrieved.dg_Udregnet.Value);
                Assert.AreEqual(Math.Round(Math.Round(bus.dg_Ticketprice.Value, currency.CurrencyPrecision.Value) * 20, 1), retrieved.dg_EndnuUdregnet.Value);
            }
        }
    }
}
#endif
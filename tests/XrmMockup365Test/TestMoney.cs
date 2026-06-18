using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using System.Linq;

namespace DG.XrmMockupTest
{
    public class TestMoney : UnitTestBase
    {
        public TestMoney(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCalculatedIsSet()
        {
            var bus = new ctx_parent
            {
                ctx_Name = "Buu"
            };
            bus.Id = orgAdminUIService.Create(bus);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(0, retrieved.ctx_AmountCalcClassic); // calc = ctx_Amount * 20; null source -> 0
            }

            bus.ctx_Name = "Woop";
            orgAdminUIService.Update(bus);
            using (var context = new Xrm(orgAdminUIService))
            {
                var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(0, retrieved.ctx_AmountCalcClassic); // calc = ctx_Amount * 20; null source -> 0
                // Dropped assertion on dg_AllConditions: no equivalent field exists on ctx_parent.
            }

            bus.ctx_Amount = 30;
            bus.ctx_WholeNumber = 5;
            bus.ctx_DateValue = DateTime.Now;
            orgAdminUIService.Update(bus);
            using (var context = new Xrm(orgAdminUIService))
            {
                var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(bus.ctx_Amount * 20, retrieved.ctx_AmountCalcClassic);
                // Dropped assertion on dg_WholeNumberUdregnetMoney: no equivalent field exists on ctx_parent.
                Assert.Equal(bus.ctx_WholeNumber - 2, retrieved.ctx_WholeNumberCalc);
                Assert.Equal(bus.ctx_DateValue.Value.AddDays(2), retrieved.ctx_DateCalc);
                Assert.Equal(bus.ctx_Name.Substring(2), retrieved.ctx_TrimLeft);
                // Dropped assertion on dg_AllConditions: no equivalent field exists on ctx_parent.
            }
        }

        [Fact]
        public void TestCalculatedIsSetRetrieveMultiple()
        {
            var bus = new ctx_parent
            {
                ctx_Name = "Buu"
            };
            bus.Id = orgAdminUIService.Create(bus);

            var q = new QueryExpression("ctx_parent");
            q.ColumnSet = new ColumnSet(true);
            var all = orgAdminService.RetrieveMultiple(q);
            Assert.Single(all.Entities);

            bus.ctx_Name = "Woop";
            orgAdminUIService.Update(bus);

            all = orgAdminService.RetrieveMultiple(q);
            var retrieved = (ctx_parent)all.Entities.Single();
            Assert.Equal(0, retrieved.ctx_AmountCalcClassic);
            // Dropped assertion on dg_AllConditions: no equivalent field exists on ctx_parent.

            bus.ctx_Amount = 30;
            bus.ctx_WholeNumber = 5;
            bus.ctx_DateValue = DateTime.Now;
            orgAdminUIService.Update(bus);

            all = orgAdminService.RetrieveMultiple(q);
            retrieved = (ctx_parent)all.Entities.Single();
            Assert.Equal(bus.ctx_Amount * 20, retrieved.ctx_AmountCalcClassic);
            Assert.Equal(bus.ctx_WholeNumber - 2, retrieved.ctx_WholeNumberCalc);
            Assert.Equal(bus.ctx_DateValue.Value.AddDays(2), retrieved.ctx_DateCalc);
            Assert.Equal(bus.ctx_Name.Substring(2), retrieved.ctx_TrimLeft);
            // Dropped assertion on dg_AllConditions: no equivalent field exists on ctx_parent.
        }

        [Fact]
        public void TestRollUp()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var childlessBus = new ctx_parent() { ctx_Name = "Buu" };
                childlessBus.Id = orgAdminUIService.Create(childlessBus);

                var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, childlessBus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Null(retrieved.ctx_TotalAllowance);

                childlessBus.ctx_Name = "Woop";
                orgAdminUIService.Update(childlessBus);

                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, childlessBus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Null(retrieved.ctx_TotalAllowance);

                var bus = new ctx_parent { ctx_Name = "Woop" };
                bus.Id = orgAdminUIService.Create(bus);

                orgAdminUIService.Create(
                    new ctx_child()
                    {
                        ctx_Name = "Hans Jørgen",
                        ctx_Allowance = 20,
                        ctx_ParentId = new EntityReference
                        {
                            Id = bus.Id,
                            LogicalName = ctx_parent.EntityLogicalName
                        }
                    });

                orgAdminUIService.Create(
                    new ctx_child()
                    {
                        ctx_Name = "Hans Gert",
                        ctx_Allowance = 50,
                        ctx_ParentId = new EntityReference
                        {
                            Id = bus.Id,
                            LogicalName = ctx_parent.EntityLogicalName
                        }
                    });

                var anotherCurrency = new TransactionCurrency()
                {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 2
                };
                anotherCurrency.Id = orgAdminUIService.Create(anotherCurrency);

                orgAdminUIService.Create(
                   new ctx_child()
                   {
                       ctx_Name = "Børge Hansen",
                       ctx_Allowance = 30,
                       ctx_ParentId = new EntityReference
                       {
                           Id = bus.Id,
                           LogicalName = ctx_parent.EntityLogicalName
                       },
                       TransactionCurrencyId = anotherCurrency.ToEntityReference()
                   });

                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Null(retrieved.ctx_TotalAllowance);
                Assert.Null(retrieved.ctx_MaxAllowance);
                Assert.Null(retrieved.ctx_MinAllowance);
                Assert.Null(retrieved.ctx_AvgAllowance);

                var req = new CalculateRollupFieldRequest()
                {
                    FieldName = "ctx_totalallowance",
                    Target = bus.ToEntityReference()
                };
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(130, retrieved.ctx_TotalAllowance);
                Assert.Null(retrieved.ctx_MaxAllowance);
                Assert.Null(retrieved.ctx_MinAllowance);
                Assert.Null(retrieved.ctx_AvgAllowance);

                req.FieldName = "ctx_maxallowance";
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(130, retrieved.ctx_TotalAllowance);
                Assert.Equal(60, retrieved.ctx_MaxAllowance);
                Assert.Null(retrieved.ctx_MinAllowance);
                Assert.Null(retrieved.ctx_AvgAllowance);

                req.FieldName = "ctx_minallowance";
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(130, retrieved.ctx_TotalAllowance);
                Assert.Equal(60, retrieved.ctx_MaxAllowance);
                Assert.Equal(20, retrieved.ctx_MinAllowance);
                Assert.Null(retrieved.ctx_AvgAllowance);

                req.FieldName = "ctx_avgallowance";
                orgAdminUIService.Execute(req);

                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(130, retrieved.ctx_TotalAllowance.Value);
                Assert.Equal(60, retrieved.ctx_MaxAllowance.Value);
                Assert.Equal(20, retrieved.ctx_MinAllowance.Value);
                Assert.Equal(43.33m, retrieved.ctx_AvgAllowance.Value);
            }
        }

        [Fact]
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

                var bus = new ctx_parent()
                {
                    ctx_Name = "Woop",
                    ctx_Amount = 10.1234m,
                    TransactionCurrencyId = currency.ToEntityReference()
                };
                bus.Id = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, bus.Id, new ColumnSet(true)) as ctx_parent;
                Assert.Equal(Math.Round(bus.ctx_Amount.Value, currency.CurrencyPrecision.Value), retrieved.ctx_Amount.Value);
                Assert.Equal(Math.Round(Math.Round(bus.ctx_Amount.Value, currency.CurrencyPrecision.Value) * 20, 2), retrieved.ctx_AmountCalcClassic.Value);
                // Dropped assertion on dg_EndnuUdregnet (1-decimal precision calculated variant): no equivalent field exists on ctx_parent.
            }
        }
    }
}

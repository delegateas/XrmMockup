using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestCurency : UnitTestBase {

        [TestMethod]
        public void TestBasesGetUpdated() {
            using (var context = new Xrm(orgAdminUIService)) {
                var currency = new TransactionCurrency() {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 2
                };
                currency.Id = orgAdminUIService.Create(currency);

                // test currency gets set
                var bus = new dg_bus();
                bus.TransactionCurrencyId = currency.ToEntityReference();
                bus.Id = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(bus.TransactionCurrencyId, retrieved.TransactionCurrencyId);

                // test base value gets updated
                bus.dg_Ticketprice = 100m;
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test currency doesn't update before record gets updated
                var oldExchangeRate = currency.ExchangeRate;
                currency.ExchangeRate = 0.7m;
                orgAdminUIService.Update(currency);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / oldExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test base value gets updated when record field value changes
                bus.dg_Ticketprice = 120m;
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test base value gets updated when transactioncurrencyid changes
                var newCurrency = new TransactionCurrency();
                newCurrency.ExchangeRate = 1m;
                newCurrency.CurrencyPrecision = 3;
                newCurrency.Id = orgAdminUIService.Create(newCurrency);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                bus.TransactionCurrencyId = newCurrency.ToEntityReference();
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / newCurrency.ExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test base value gets updated when state of record changes
                oldExchangeRate = newCurrency.ExchangeRate;
                newCurrency.ExchangeRate = 0.3m;
                orgAdminUIService.Update(newCurrency);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / oldExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                bus.SetState(orgAdminUIService, dg_busState.Inactive);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / newCurrency.ExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

            }
        }

        [TestMethod]
        public void TestExchangeRate() {
            using (var context = new Xrm(orgAdminUIService)) {
                var dollar = new TransactionCurrency();
                dollar.ExchangeRate = 0.6m;
                dollar.CurrencyPrecision = 3;
                var dollarId = orgAdminUIService.Create(dollar);
                dollar.Id = dollarId;


                var bus = new dg_bus();
                bus.dg_Ticketprice = 10m;
                bus.TransactionCurrencyId = dollar.ToEntityReference();
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.IsTrue(retrieved.dg_ticketprice_Base.HasValue);
                Assert.AreEqual(Math.Round(bus.dg_Ticketprice.Value / dollar.ExchangeRate.Value, dollar.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base.Value);
            }
        }


        [TestMethod]
        public void TestExchangeIsSet() {
            using (var context = new Xrm(orgAdminUIService)) {
                var dollar = new TransactionCurrency();
                dollar.ExchangeRate = 0.6m;
                dollar.CurrencyPrecision = 3;
                var dollarId = orgAdminUIService.Create(dollar);
                dollar.Id = dollarId;


                var bus = new dg_bus();
                bus.dg_Ticketprice = 10m;
                bus.TransactionCurrencyId = dollar.ToEntityReference();
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.IsTrue(retrieved.ExchangeRate.HasValue);
                Assert.AreEqual(dollar.ExchangeRate, retrieved.ExchangeRate);
            }
        }

        [TestMethod]
        public void TestSettingTransactionCurrency() {
            using (var context = new Xrm(orgAdminUIService)) {
                var bus = new dg_bus();
                var busId = orgAdminUIService.Create(bus);
                var query = new QueryExpression(TransactionCurrency.EntityLogicalName);
                query.Criteria = new FilterExpression();
                query.Criteria.AddCondition(new ConditionExpression("isocurrencycode", ConditionOperator.Equal, "DKK"));
                var resp = orgAdminUIService.RetrieveMultiple(query);
                var currency = resp.Entities.First().ToEntity<TransactionCurrency>();

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(currency.ToEntityReference().Id, retrieved.TransactionCurrencyId.Id);

                bus.dg_Ticketprice = 10m;
                orgAdminUIService.Update(bus);
                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(currency.ToEntityReference().Id, retrieved.TransactionCurrencyId.Id);
            }
        }

        [TestMethod]
        public void TestUserCurrencyIsChosen() {
            using (var context = new Xrm(orgAdminUIService)) {
                var currentUser = orgAdminUIService.Retrieve(SystemUser.EntityLogicalName, crm.AdminUser.Id, new ColumnSet(true)).ToEntity<SystemUser>();
                var currency = new TransactionCurrency();
                currency.ExchangeRate = 0.5m;
                currency.CurrencyPrecision = 3;
                currency.Id = orgAdminUIService.Create(currency);
                currentUser.TransactionCurrencyId = currency.ToEntityReference();
                orgAdminUIService.Update(currentUser);

                var bus = new dg_bus();
                bus.dg_Ticketprice = 10m;
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(currency.ToEntityReference(), retrieved.TransactionCurrencyId);

                bus.dg_Ticketprice = 20m;
                orgAdminUIService.Update(bus);
                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.AreEqual(currency.ToEntityReference(), retrieved.TransactionCurrencyId);
            }
        }
    }

}

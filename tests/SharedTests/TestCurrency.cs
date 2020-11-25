using System;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestCurrency : UnitTestBase
    {
        public TestCurrency(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestBasesGetUpdated()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var currency = new TransactionCurrency()
                {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 2
                };
                currency.Id = orgAdminUIService.Create(currency);

                // test currency gets set
                var bus = new dg_bus
                {
                    TransactionCurrencyId = currency.ToEntityReference()
                };
                bus.Id = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(bus.TransactionCurrencyId, retrieved.TransactionCurrencyId);

                // test base value gets updated
                bus.dg_Ticketprice = 100m;
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test currency doesn't update before record gets updated
                var oldExchangeRate = currency.ExchangeRate;
                currency.ExchangeRate = 0.7m;
                orgAdminUIService.Update(currency);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / oldExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test base value gets updated when record field value changes
                bus.dg_Ticketprice = 120m;
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test base value gets updated when transactioncurrencyid changes
                var newCurrency = new TransactionCurrency
                {
                    ExchangeRate = 1m,
                    CurrencyPrecision = 3
                };
                newCurrency.Id = orgAdminUIService.Create(newCurrency);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                bus.TransactionCurrencyId = newCurrency.ToEntityReference();
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / newCurrency.ExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                // test base value gets updated when state of record changes
                oldExchangeRate = newCurrency.ExchangeRate;
                newCurrency.ExchangeRate = 0.3m;
                orgAdminUIService.Update(newCurrency);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / oldExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

                bus.SetState(orgAdminUIService, dg_busState.Inactive);

                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, bus.Id, new ColumnSet(true)) as dg_bus;
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / newCurrency.ExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base);

            }
        }

        [Fact]
        public void TestExchangeRate()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var dollar = new TransactionCurrency
                {
                    ExchangeRate = 0.6m,
                    CurrencyPrecision = 3
                };
                var dollarId = orgAdminUIService.Create(dollar);
                dollar.Id = dollarId;


                var bus = new dg_bus
                {
                    dg_Ticketprice = 10m,
                    TransactionCurrencyId = dollar.ToEntityReference()
                };
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.True(retrieved.dg_ticketprice_Base.HasValue);
               Assert.Equal(Math.Round(bus.dg_Ticketprice.Value / dollar.ExchangeRate.Value, dollar.CurrencyPrecision.Value), retrieved.dg_ticketprice_Base.Value);
            }
        }

        [Fact]
        public void TestExchangeIsSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var dollar = new TransactionCurrency
                {
                    ExchangeRate = 0.6m,
                    CurrencyPrecision = 3
                };
                var dollarId = orgAdminUIService.Create(dollar);
                dollar.Id = dollarId;


                var bus = new dg_bus
                {
                    dg_Ticketprice = 10m,
                    TransactionCurrencyId = dollar.ToEntityReference()
                };
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
                Assert.True(retrieved.ExchangeRate.HasValue);
               Assert.Equal(dollar.ExchangeRate, retrieved.ExchangeRate);
            }
        }

        [Fact]
        public void TestSettingTransactionCurrency()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var bus = new dg_bus();
                var busId = orgAdminUIService.Create(bus);
                var query = new QueryExpression(TransactionCurrency.EntityLogicalName)
                {
                    Criteria = new FilterExpression()
                };
                query.Criteria.AddCondition(new ConditionExpression("isocurrencycode", ConditionOperator.Equal, "DKK"));
                var resp = orgAdminUIService.RetrieveMultiple(query);
                var currency = resp.Entities.First().ToEntity<TransactionCurrency>();

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
               Assert.Equal(currency.ToEntityReference().Id, retrieved.TransactionCurrencyId.Id);

                bus.dg_Ticketprice = 10m;
                orgAdminUIService.Update(bus);
                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
               Assert.Equal(currency.ToEntityReference().Id, retrieved.TransactionCurrencyId.Id);
            }
        }

        [Fact]
        public void TestUserCurrencyIsChosen()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var currentUser = orgAdminUIService.Retrieve(SystemUser.EntityLogicalName, crm.AdminUser.Id, new ColumnSet(true)).ToEntity<SystemUser>();
                var currency = new TransactionCurrency
                {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 3
                };
                currency.Id = orgAdminUIService.Create(currency);
                currentUser.TransactionCurrencyId = currency.ToEntityReference();
                orgAdminUIService.Update(currentUser);

                var bus = new dg_bus
                {
                    dg_Ticketprice = 10m
                };
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
               Assert.Equal(currency.ToEntityReference(), retrieved.TransactionCurrencyId);

                bus.dg_Ticketprice = 20m;
                orgAdminUIService.Update(bus);
                retrieved = orgAdminUIService.Retrieve(dg_bus.EntityLogicalName, busId, new ColumnSet(true)) as dg_bus;
               Assert.Equal(currency.ToEntityReference(), retrieved.TransactionCurrencyId);
            }
        }

        [Fact]
        public void TestRetrieveExchangeRate()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var dollar = new TransactionCurrency
                {
                    ExchangeRate = 0.6m
                };
                dollar.Id = orgAdminUIService.Create(dollar);

                var request = new RetrieveExchangeRateRequest
                {
                    TransactionCurrencyId = dollar.Id
                };
                var response = orgAdminUIService.Execute(request) as RetrieveExchangeRateResponse;
               Assert.Equal(dollar.ExchangeRate, response.ExchangeRate);
            }
        }

        [Fact]
        public void TestRetriveExhangeRateFail()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var request = new RetrieveExchangeRateRequest
                {
                    TransactionCurrencyId = Guid.NewGuid()
                };
                try
                {
                    orgAdminUIService.Execute(request);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }
    }

}

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestQuote : UnitTestBase
    {
        public TestQuote(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestWinQuoteUsingStandardStatus()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var quote = new Quote();
                quote.Id = orgAdminUIService.Create(quote);

                var winReq = new WinQuoteRequest()
                {
                    QuoteClose = new QuoteClose()
                    {
                        QuoteId = quote.ToEntityReference(),
                        Subject = "Quote won"
                    },
                    Status = new OptionSetValue(-1) // default to Won
                };

                orgAdminUIService.Execute(winReq);

                var retrieved = orgAdminUIService.Retrieve(Quote.EntityLogicalName, quote.Id, new ColumnSet(true)) as Quote;

                Assert.Equal(QuoteState.Won, retrieved.StateCode);
                Assert.Equal(Quote_StatusCode.Won, retrieved.StatusCode);
            }
        }

        [Fact]
        public void TestWinQuote()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var quote = new Quote();
                quote.Id = orgAdminUIService.Create(quote);

                var winReq = new WinQuoteRequest()
                {
                    QuoteClose = new QuoteClose()
                    {
                        QuoteId = quote.ToEntityReference(),
                        Subject = "Quote won"
                    },
                    Status = new OptionSetValue((int)Quote_StatusCode.Won)
                };

                orgAdminUIService.Execute(winReq);

                var retrieved = orgAdminUIService.Retrieve(Quote.EntityLogicalName, quote.Id, new ColumnSet(true)) as Quote;

                Assert.Equal(QuoteState.Won, retrieved.StateCode);
                Assert.Equal(Quote_StatusCode.Won, retrieved.StatusCode);
            }
        }

        [Fact]
        public void TestCloseQuoteUsingStandardStatus()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var quote = new Quote();
                quote.Id = orgAdminUIService.Create(quote);

                var winReq = new CloseQuoteRequest()
                {
                    QuoteClose = new QuoteClose()
                    {
                        QuoteId = quote.ToEntityReference(),
                        Subject = "Quote closed"
                    },
                    Status = new OptionSetValue(-1) // default to Lost
                };

                orgAdminUIService.Execute(winReq);

                var retrieved = orgAdminUIService.Retrieve(Quote.EntityLogicalName, quote.Id, new ColumnSet(true)) as Quote;

                Assert.Equal(QuoteState.Closed, retrieved.StateCode);
                Assert.Equal(Quote_StatusCode.Lost, retrieved.StatusCode);
            }
        }

        [Fact]
        public void TestCloseQuote()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var quote = new Quote();
                quote.Id = orgAdminUIService.Create(quote);

                var winReq = new CloseQuoteRequest()
                {
                    QuoteClose = new QuoteClose()
                    {
                        QuoteId = quote.ToEntityReference(),
                        Subject = "Quote closed"
                    },
                    Status = new OptionSetValue((int)Quote_StatusCode.Lost)
                };

                orgAdminUIService.Execute(winReq);

                var retrieved = orgAdminUIService.Retrieve(Quote.EntityLogicalName, quote.Id, new ColumnSet(true)) as Quote;

                Assert.Equal(QuoteState.Closed, retrieved.StateCode);
                Assert.Equal(Quote_StatusCode.Lost, retrieved.StatusCode);
            }
        }
    }
}

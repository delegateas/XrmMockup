using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using System.Linq;

namespace DG.XrmMockupTest
{
    public class TestTracingService : UnitTestBase
    {
        public TestTracingService(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TracingServiceShouldAcceptXMLFormatStrings()
        {
            ITracingService trace = new TracingService();
            trace.Trace("cams_signatures : <head><style type=text/css>p,li{font-family: Arial,Helvetica,sans-serif;font-size: 11pt;}</style></head><body><p contenteditable=\"false\"><span style=\font - family: Arial; font - size: 11pt; \">By signing the Agreement Plan, the signatories are certifying, subject to the exceptions detailed below, both the completeness of the Agreement Plan and the agreed commitment between [CUSTOMER] and [DELIVERY AGENT].</span></p></body>");
        }

        [Fact]
        public void TracingServiceShouldEvaluateFormatStringWithArgs()
        {
            ITracingService trace = new TracingService();
            trace.Trace("Value is {0} and {1}", 42, "test");
        }

        [Fact]
        public void TracingServiceShouldThrowOnInvalidFormatStringWithArgs()
        {
            ITracingService trace = new TracingService();
            // Referencing an argument index that isn't supplied would throw a FormatException
            // in a real environment; the mockup must surface it rather than swallow it.
            Assert.Throws<System.FormatException>(() => trace.Trace("Value is {1}", 42));
        }

        [Fact]
        public void TraceMessagesAreCollectedAndExposedForAssertions()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountTracePlugin));
            crm.ClearTraceLog();

            var account = new Account { Name = "Contoso" };
            account.Id = orgAdminService.Create(account);

            Assert.Contains("Creating account", crm.TraceLog);
            Assert.Contains("Account name is Contoso", crm.TraceLog);
        }

        [Fact]
        public void ClearTraceLogEmptiesCollectedMessages()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountTracePlugin));

            var account = new Account { Name = "Contoso" };
            account.Id = orgAdminService.Create(account);
            Assert.NotEmpty(crm.TraceLog);

            crm.ClearTraceLog();
            Assert.Empty(crm.TraceLog);
        }
    }
}

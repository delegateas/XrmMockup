using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;

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
    }
}

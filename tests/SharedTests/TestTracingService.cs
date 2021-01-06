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
            trace.Trace("<xml>formatted</xml>");
            

            
        }
    }
}

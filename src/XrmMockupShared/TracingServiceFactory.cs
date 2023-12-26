using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup
{
    internal class TracingServiceFactory
    {
        private readonly Func<ITracingService> _factory;

        public TracingServiceFactory(Func<ITracingService> factory)
        {
            _factory = factory;
        }

        public ITracingService GetService() => _factory?.Invoke() ?? new TracingService();
    }
}

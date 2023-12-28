using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup
{
    public interface ITracingServiceFactory
    {
        ITracingService GetService();
    }

    internal class TracingServiceFactory : ITracingServiceFactory
    {
        public ITracingService GetService() => new TracingService();
    }
}

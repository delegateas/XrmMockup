using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {
    internal class TracingService : ITracingService
    {
        static TracingService()
        {
            //Enable mstest output to test results by using Trace.
            System.Diagnostics.Trace.Listeners.Add(new ConsoleTraceListener());
        }
        public void Trace(string format, params object[] args)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(string.Format(format, args));
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine(format);
            }
        }
    }
}

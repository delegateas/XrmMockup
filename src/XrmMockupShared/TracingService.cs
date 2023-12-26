using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup {
    internal sealed class TracingService : ITracingService {
        public void Trace(string format, params object[] args) {
			try
			{
				Console.WriteLine(format, args);
			}
			catch
			{
				Console.WriteLine(format);
			}
        }
    }
}

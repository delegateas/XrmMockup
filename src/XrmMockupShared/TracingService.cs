using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {
    internal class TracingService : ITracingService {
        public void Trace(string format, params object[] args) {
			try
			{
				Console.WriteLine(format, args);
			}
			catch
			{


			}
		}
    }
}

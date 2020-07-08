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
			catch (Exception ex)
			{
				if (ex.Message.Contains("correct format")) {
					format = new string(format.Where(c => !char.IsPunctuation(c)).ToArray());
					Console.WriteLine(format,args);
				}
				else {
					throw;
				}
				
			}
            
        }
    }
}

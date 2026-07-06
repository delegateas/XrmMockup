using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup
{
	internal sealed class TracingService : ITracingService
	{
		private readonly Action<string> _sink;

		public TracingService() : this(null) { }

		/// <param name="sink">Optional callback invoked with each fully-evaluated trace message, allowing a consumer to collect them for assertions.</param>
		public TracingService(Action<string> sink)
		{
			_sink = sink;
		}

		public void Trace(string format, params object[] args)
		{
			// Match the platform's sandbox tracing service: a message supplied without
			// arguments is emitted verbatim (so literal braces in e.g. XML/HTML are fine),
			// while a message with arguments is evaluated through String.Format. We do NOT
			// swallow a resulting FormatException, so invalid format strings surface during
			// testing instead of silently slipping through to production.
			var message = (args == null || args.Length == 0)
				? format
				: string.Format(format, args);

			Console.WriteLine(message);
			_sink?.Invoke(message);
		}
	}
}

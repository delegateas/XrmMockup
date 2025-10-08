using XrmPluginCore;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy.XrmPluginCore
{
    internal class PluginRegistrationStrategy : IRegistrationStrategy<IPluginStepConfig>
    {
        public IEnumerable<IPluginStepConfig> AnalyzeType(IPlugin plugin)
        {
            if (plugin is IPluginDefinition pluginDefinition)
            {
                return pluginDefinition.GetRegistrations();
            }

            return Enumerable.Empty<IPluginStepConfig>();
        }
    }
}
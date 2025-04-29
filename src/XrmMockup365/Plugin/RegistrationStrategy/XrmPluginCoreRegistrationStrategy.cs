using DG.XrmPluginCore;
using DG.XrmPluginCore.Interfaces.CustomApi;
using DG.XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    internal class XrmPluginCoreRegistrationStrategy : IRegistrationStrategy
    {
        public IEnumerable<IPluginStepConfig> GetPluginRegistrations(Type basePluginType, IPlugin plugin, List<MetaPlugin> metaPlugins)
        {
            if (plugin is IPluginDefinition pluginDefinition)
            {
                return pluginDefinition.GetRegistrations();
            }

            throw new MockupException($"Plugin '{basePluginType.FullName}' does not implement {nameof(IPluginDefinition)}.");
        }

        public ICustomApiConfig GetCustomApiRegistration(Type pluginType, IPlugin plugin)
        {
            if (plugin is ICustomApiDefinition customApiDefinition)
            {
                return customApiDefinition.GetRegistration();
            }

            throw new MockupException($"Plugin '{pluginType.FullName}' does not implement {nameof(ICustomApiDefinition)}.");
        }

        public bool IsValidForPlugin(Type pluginType) => pluginType.GetInterfaces().Any(i => i == typeof(IPluginDefinition));

        public bool IsValidForCustomApi(Type pluginType) => pluginType.GetInterfaces().Any(i => i == typeof(ICustomApiDefinition));
    }
}
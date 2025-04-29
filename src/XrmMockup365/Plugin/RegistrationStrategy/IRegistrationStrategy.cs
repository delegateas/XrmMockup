using DG.XrmPluginCore.Interfaces.CustomApi;
using DG.XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    internal interface IRegistrationStrategy
    {
        IEnumerable<IPluginStepConfig> GetPluginRegistrations(Type pluginType, IPlugin plugin, List<MetaPlugin> metaPlugins);
        ICustomApiConfig GetCustomApiRegistration(Type pluginType, IPlugin plugin);

        bool IsValidForPlugin(Type pluginType);
        bool IsValidForCustomApi(Type pluginType);
    }
}

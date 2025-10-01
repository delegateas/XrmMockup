using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;
using XrmPluginCore;
using XrmPluginCore.Interfaces.CustomApi;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy.XrmPluginCore
{
    internal class CustomApiRegistrationStrategy : IRegistrationStrategy<ICustomApiConfig>
    {
        public IEnumerable<ICustomApiConfig> AnalyzeType(IPlugin plugin)
        {
            if (plugin is ICustomApiDefinition customApiDefinition)
            {
                yield return customApiDefinition.GetRegistration();
            }
        }
    }
}
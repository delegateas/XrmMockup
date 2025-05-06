using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.Linq;
using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;

namespace DG.Tools.XrmMockup
{
    internal class CustomApiManager
    {

        private Dictionary<string, Action<MockupServiceProviderAndFactory>> registeredApis = new Dictionary<string, Action<MockupServiceProviderAndFactory>>();
        private readonly List<IRegistrationStrategy> registrationStrategies = new List<IRegistrationStrategy>
        {
            new XrmPluginCoreRegistrationStrategy(),
            new LegacyRegistrationStrategy()
        };

        public CustomApiManager(IEnumerable<Tuple<string, Type>> baseCustomApiTypes)
        {
            if (baseCustomApiTypes == null)
            {
                return;
            }

            RegisterCustomApis(baseCustomApiTypes);
        }

        private void RegisterCustomApis(IEnumerable<Tuple<string, Type>> customApiBaseTypeMappings)
        {
            foreach (var customApiMapping in customApiBaseTypeMappings)
            {
                if (customApiMapping == null) continue;

                var prefix = customApiMapping.Item1;
                var baseApiType = customApiMapping.Item2;

                var customApiTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetLoadableTypes().Where(t => !t.IsAbstract && t.IsPublic && t.BaseType != null && (t.BaseType == baseApiType || (t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == baseApiType))))
                    .ToList();

                foreach (var type in customApiTypes)
                {
                    RegisterApi(prefix, type);
                }
            }
        }

        private void RegisterApi(string prefix, Type pluginType)
        {
            var plugin = Utility.CreatePluginInstance(pluginType);
            if (plugin == null)
            {
                return;
            }

            var registrationStrategy = registrationStrategies.FirstOrDefault(s => s.IsValidForCustomApi(pluginType)) ??
                throw new MockupException($"No registration strategy found for CustomApi '{plugin.GetType().FullName}'");

            var registration = registrationStrategy.GetCustomApiRegistration(pluginType, plugin);
            registeredApis.Add($"{prefix}_{registration.UniqueName}", plugin.Execute);
        }

        public bool HandlesRequest(string requestName)
        {
            return registeredApis.ContainsKey(requestName);
        }

        internal OrganizationResponse Execute(OrganizationRequest request, Core core, PluginContext pluginContext)
        {
            if (!registeredApis.ContainsKey(request.RequestName))
            {
                throw new FaultException($"Request '{request.RequestName}' is not a registered Custom API");
            }

            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Stage = 30;
            thisPluginContext.Mode = 0;

            var serviceProvider = new MockupServiceProviderAndFactory(core, thisPluginContext, core.TracingServiceFactory);
            registeredApis[request.RequestName](serviceProvider);
            
            pluginContext.OutputParameters.Clear();
            pluginContext.OutputParameters.AddRange(thisPluginContext.OutputParameters);

            return new OrganizationResponse()
            {
                Results = pluginContext.OutputParameters
            };
        }
    }
}
using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;

namespace DG.Tools.XrmMockup
{
    internal class CustomApiManager
    {

        // Static caches shared across all CustomApiManager instances
        private static readonly ConcurrentDictionary<string, Dictionary<string, Action<MockupServiceProviderAndFactory>>> _cachedApis = new ConcurrentDictionary<string, Dictionary<string, Action<MockupServiceProviderAndFactory>>>();
        private static readonly ConcurrentDictionary<Type, IPlugin> _apiInstanceCache = new ConcurrentDictionary<Type, IPlugin>();
        private static readonly object _cacheLock = new object();

        private Dictionary<string, Action<MockupServiceProviderAndFactory>> registeredApis = new Dictionary<string, Action<MockupServiceProviderAndFactory>>();

        private readonly List<IRegistrationStrategy> registrationStrategies = new List<IRegistrationStrategy>
        {
            new XrmPluginCoreRegistrationStrategy(),
            new LegacyRegistrationStrategy()
        };

        public CustomApiManager(IEnumerable<Tuple<string, Type>> baseCustomApiTypes)
        {
            var cacheKey = GenerateApiCacheKey(baseCustomApiTypes);

            // Check if we have cached results
            if (_cachedApis.ContainsKey(cacheKey))
            {
                // Use cached results - no reflection/instantiation needed
                registeredApis = new Dictionary<string, Action<MockupServiceProviderAndFactory>>(_cachedApis[cacheKey]);
                return;
            }

            lock (_cacheLock)
            {
                // Double-check locking pattern
                if (_cachedApis.ContainsKey(cacheKey))
                {
                    registeredApis = new Dictionary<string, Action<MockupServiceProviderAndFactory>>(_cachedApis[cacheKey]);
                    return;
                }

                // First time - do the work and cache it
                registeredApis = new Dictionary<string, Action<MockupServiceProviderAndFactory>>();
                if (baseCustomApiTypes != null)
                {
                    RegisterCustomApis(baseCustomApiTypes);
                }

                // Cache for future instances
                _cachedApis[cacheKey] = new Dictionary<string, Action<MockupServiceProviderAndFactory>>(registeredApis);
            }
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
            var plugin = _apiInstanceCache.GetOrAdd(pluginType, Utility.CreatePluginInstance);

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

        private string GenerateApiCacheKey(IEnumerable<Tuple<string, Type>> baseCustomApiTypes)
        {
            if (baseCustomApiTypes == null) return "null_types";

            var typeKeys = baseCustomApiTypes.Where(t => t != null && t.Item2 != null)
                .Select(t => $"{t.Item1}:{t.Item2.FullName}")
                .OrderBy(k => k);
            var combinedTypes = string.Join("|", typeKeys);

            if (string.IsNullOrEmpty(combinedTypes)) return "empty_types";

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedTypes));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using System.Runtime.ExceptionServices;
using XrmMockupShared.Plugin;

using MainCustomAPIConfig = System.Tuple<string, bool, int, int, int, string>;
using ExtendedCustomAPIConfig = System.Tuple<string, string, string, bool, bool, string, string>;
using RequestParameterConfig = System.Tuple<string, string, string, bool, bool, string, int>; // TODO: Add description maybe
using ResponsePropertyConfig = System.Tuple<string, string, string, bool, string, int>; // TODO

namespace DG.Tools.XrmMockup
{
    internal class CustomApiManager
    {

        private Dictionary<string, Func<MockupServiceProviderAndFactory, OrganizationResponse>> registeredApis;

        public CustomApiManager(IEnumerable<Tuple<string, Type>> baseCustomApiTypes)
        {
            registeredApis = new Dictionary<string, Func<MockupServiceProviderAndFactory, OrganizationResponse>>();
            if (baseCustomApiTypes == null)
            {
                return;
            }

            RegisterCustomApis(baseCustomApiTypes);
        }

        private void RegisterCustomApis(IEnumerable<Tuple<string, Type>> baseCustomApiTypes)
        {
            foreach (var baseType in baseCustomApiTypes)
            {
                var prefix = baseType.Item1;
                var baseApiType = baseType.Item2;
                if (baseType == null) continue;
                Assembly proxyTypeAssembly = baseApiType.Assembly;

                foreach (var type in proxyTypeAssembly.GetLoadableTypes())
                {
                    if (type.BaseType != null && (type.BaseType == baseApiType || (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == baseApiType)))
                    {
                        RegisterApi(prefix, type);
                    }
                }
            }
        }

        private void RegisterApi(string prefix, Type baseType)
        {
            object plugin = null;
            try
            {
                plugin = Activator.CreateInstance(baseType);
            }
            catch (Exception ex) when (ex.Source == "mscorlib" || ex.Source == "System.Private.CoreLib")
            {
            }

            if (plugin == null)
            {
                return;
            }

            // Check if Matches DAXIF plugin registration
            if (baseType.GetMethod("GetCustomAPIConfig") == null)
                return;

            var configs = baseType
                .GetMethod("GetCustomAPIConfig")
                .Invoke(plugin, new object[] { })
                as Tuple<MainCustomAPIConfig, ExtendedCustomAPIConfig, IEnumerable<RequestParameterConfig>, IEnumerable<ResponsePropertyConfig>>;

            Func<MockupServiceProviderAndFactory, OrganizationResponse> pluginExecute = (provider) =>
            {
                var resp =
                    baseType
                    .GetMethod("Execute")
                    .Invoke(plugin, new object[] { provider });

                return resp as OrganizationResponse;
            };

            registeredApis.Add($"{prefix}_{configs.Item1.Item1}", pluginExecute);
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
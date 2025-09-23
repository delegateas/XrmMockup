﻿using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.CustomApi;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy.DAXIF
{
    // MainCustomAPIConfig      : UniqueName, IsFunction, EnabledForWorkflow, AllowedCustomProcessingStepType, BindingType, BoundEntityLogicalName
    // ExtendedCustomAPIConfig  : PluginType, OwnerId, OwnerType, IsCustomizable, IsPrivate, ExecutePrivilegeName, Description
    // RequestParameterConfig   : Name, UniqueName, DisplayName, IsCustomizable, IsOptional, LogicalEntityName, Type
    // ResponsePropertyConfig   : Name, UniqueName, DisplayName, IsCustomizable, LogicalEntityName, Type
    using MainCustomAPIConfig = Tuple<string, bool, int, int, int, string>;
    using ExtendedCustomAPIConfig = Tuple<string, string, string, bool, bool, string, string>;
    using RequestParameterConfig = Tuple<string, string, string, bool, bool, string, int>; // TODO: Add description maybe
    using ResponsePropertyConfig = Tuple<string, string, string, bool, string, int>; // TODO

    internal class CustomApiRegistrationStrategy : IRegistrationStrategy<ICustomApiConfig>
    {
        public IEnumerable<ICustomApiConfig> AnalyzeType(IPlugin plugin)
        {
            var pluginType = plugin.GetType();
            if (pluginType.GetMethod("GetCustomAPIConfig") == null)
            {
                yield break;
            }

            var configs = pluginType
                .GetMethod("GetCustomAPIConfig")
                .Invoke(plugin, new object[] { })
                as Tuple<MainCustomAPIConfig, ExtendedCustomAPIConfig, IEnumerable<RequestParameterConfig>, IEnumerable<ResponsePropertyConfig>>;

            yield return new CustomApiConfig
            {
                UniqueName = configs.Item1.Item1,
                IsFunction = configs.Item1.Item2,
                EnabledForWorkflow = configs.Item1.Item3 == 1,
                AllowedCustomProcessingStepType = (AllowedCustomProcessingStepType)configs.Item1.Item4,
                BindingType = (BindingType)configs.Item1.Item5,
                BoundEntityLogicalName = configs.Item1.Item6,
                PluginType = configs.Item2.Item1,
                OwnerId = configs.Item2.Item2,
                OwnerType = configs.Item2.Item3,
                IsCustomizable = configs.Item2.Item4,
                IsPrivate = configs.Item2.Item5,
                ExecutePrivilegeName = configs.Item2.Item6,
                Description = configs.Item2.Item7,
                RequestParameters = configs.Item3.Select(r => new RequestParameter
                {
                    Name = r.Item1,
                    UniqueName = r.Item2,
                    DisplayName = r.Item3,
                    IsCustomizable = r.Item4,
                    IsOptional = r.Item5,
                    LogicalEntityName = r.Item6,
                    TypeCode = (AttributeTypeCode)r.Item7
                }),
                ResponseProperties = configs.Item4.Select(r => new ResponseProperty
                {
                    Name = r.Item1,
                    UniqueName = r.Item2,
                    DisplayName = r.Item3,
                    IsCustomizable = r.Item4,
                    LogicalEntityName = r.Item5,
                    TypeCode = (AttributeTypeCode)r.Item6
                })
            };
        }
    }
}

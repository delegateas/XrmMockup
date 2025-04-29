using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.CustomApi;
using DG.XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes,impersonating user id
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

    // MainCustomAPIConfig      : UniqueName, IsFunction, EnabledForWorkflow, AllowedCustomProcessingStepType, BindingType, BoundEntityLogicalName
    // ExtendedCustomAPIConfig  : PluginType, OwnerId, OwnerType, IsCustomizable, IsPrivate, ExecutePrivilegeName, Description
    // RequestParameterConfig   : Name, UniqueName, DisplayName, IsCustomizable, IsOptional, LogicalEntityName, Type
    // ResponsePropertyConfig   : Name, UniqueName, DisplayName, IsCustomizable, LogicalEntityName, Type
    using MainCustomAPIConfig = Tuple<string, bool, int, int, int, string>;
    using ExtendedCustomAPIConfig = Tuple<string, string, string, bool, bool, string, string>;
    using RequestParameterConfig = Tuple<string, string, string, bool, bool, string, int>; // TODO: Add description maybe
    using ResponsePropertyConfig = Tuple<string, string, string, bool, string, int>; // TODO

    internal class LegacyRegistrationStrategy : IRegistrationStrategy
    {
        public ICustomApiConfig GetCustomApiRegistration(Type pluginType, IPlugin plugin)
        {
            var configs = pluginType
                .GetMethod("GetCustomAPIConfig")
                .Invoke(plugin, new object[] { })
                as Tuple<MainCustomAPIConfig, ExtendedCustomAPIConfig, IEnumerable<RequestParameterConfig>, IEnumerable<ResponsePropertyConfig>>;

            return new CustomApiConfig
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
                ResponseParameters = configs.Item4.Select(r => new ResponseProperty
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

        public IEnumerable<IPluginStepConfig> GetPluginRegistrations(Type basePluginType, IPlugin plugin, List<MetaPlugin> metaPlugins)
        {
            var configs = basePluginType
                .GetMethod("PluginProcessingStepConfigs")
                .Invoke(plugin, new object[] { })
                as IEnumerable<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>;

            return configs.Select(c =>
            {
                if (!Enum.TryParse<EventOperation>(c.Item1.Item3, true, out var eventOperation))
                {
                    throw new MockupException($"Unknown message '{c.Item1.Item3}' for plugin '{plugin.GetType().FullName}'");
                }

                var hasImpersonatingUser = Guid.TryParse(c.Item2.Item6, out var impersonatingUserId);

                return new PluginStepConfig
                {
                    ExecutionStage = (ExecutionStage)c.Item1.Item2,
                    EventOperation = eventOperation,
                    EntityLogicalName = c.Item1.Item4,
                    Deployment = (Deployment)c.Item2.Item1,
                    ExecutionMode = (ExecutionMode)c.Item2.Item2,
                    Name = c.Item2.Item3,
                    ExecutionOrder = c.Item2.Item4,
                    FilteredAttributes = c.Item2.Item5,
                    ImpersonatingUserId = hasImpersonatingUser ? impersonatingUserId : (Guid?)null,
                    ImageSpecifications = c.Item3.Select(i => new ImageSpecification
                    {
                        ImageName = i.Item1,
                        EntityAlias = i.Item2,
                        ImageType = (ImageType)i.Item3,
                        Attributes = i.Item4
                    })
                };
            });
        }

        public bool IsValidForCustomApi(Type pluginType) => pluginType.GetMethod("GetCustomAPIConfig") != null;

        public bool IsValidForPlugin(Type pluginType) => pluginType.GetMethod("PluginProcessingStepConfigs") != null;
    }
}

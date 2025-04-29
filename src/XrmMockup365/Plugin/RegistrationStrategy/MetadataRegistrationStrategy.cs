using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.CustomApi;
using DG.XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    internal class MetadataRegistrationStrategy : IRegistrationStrategy
    {
        public IEnumerable<IPluginStepConfig> GetPluginRegistrations(Type basePluginType, IPlugin plugin, List<MetaPlugin> metaPlugins)
        {
            // Retrieve registration from CRM metadata
            var metaSteps =
                metaPlugins
                .Where(x =>
                    x.AssemblyName == basePluginType.FullName &&
                    x.PluginTypeAssemblyName == basePluginType.Assembly.GetName().Name)
                .ToList();

            // fallback for backwards compatability for old Metadata files
            if (metaSteps == null || metaSteps.Count == 0)
            {
                metaSteps =
                    metaPlugins
                    .Where(x => x.AssemblyName == basePluginType.FullName)
                    .ToList();
            }

            if (metaSteps == null || metaSteps.Count == 0)
            {
                throw new MockupException($"Unknown plugin '{basePluginType.FullName}', please use DAXIF registration or make sure the plugin is uploaded to CRM.");
            }

            foreach (var metaStep in metaSteps)
            {
                if (!Enum.TryParse<EventOperation>(metaStep.MessageName, true, out var eventOperation))
                {
                    throw new MockupException($"Unknown message '{metaStep.MessageName}' for plugin '{basePluginType.FullName}'");
                }

                yield return new PluginStepConfig
                {
                    ExecutionStage = (ExecutionStage)metaStep.Stage,
                    EventOperation = eventOperation,
                    EntityLogicalName = metaStep.PrimaryEntity,
                    Deployment = 0,
                    ExecutionMode = (ExecutionMode)metaStep.Mode,
                    Name = metaStep.Name,
                    ExecutionOrder = metaStep.Rank,
                    FilteredAttributes = metaStep.FilteredAttributes,
                    ImpersonatingUserId = metaStep.ImpersonatingUserId ?? (Guid?)null,
                    AsyncAutoDelete = metaStep.AsyncAutoDelete,
                    ImageSpecifications = metaStep.Images?.Select(x => new ImageSpecification
                    {
                        ImageName = x.Name,
                        EntityAlias = x.EntityAlias,
                        ImageType = (ImageType)x.ImageType,
                        Attributes = x.Attributes
                    }) ?? Enumerable.Empty<IImageSpecification>()
                };
            }
        }

        public bool IsValidForPlugin(Type pluginType) => true;

        public bool IsValidForCustomApi(Type pluginType) => false;
        public ICustomApiConfig GetCustomApiRegistration(Type pluginType, IPlugin plugin) => throw new NotImplementedException();
    }
}

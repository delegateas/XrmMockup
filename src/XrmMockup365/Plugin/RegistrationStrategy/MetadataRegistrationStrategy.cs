using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    internal class MetadataRegistrationStrategy
    {
        public IEnumerable<IPluginStepConfig> AnalyzeType(Type pluginType, List<MetaPlugin> metaPlugins)
        {
            // Retrieve registration from CRM metadata
            var metaSteps =
                metaPlugins
                .Where(x =>
                    x.AssemblyName == pluginType.FullName &&
                    x.PluginTypeAssemblyName == pluginType.Assembly.GetName().Name);

            // Fallback for backwards compatability for old Metadata files
            if (!metaSteps.Any())
            {
                metaSteps =
                    metaPlugins
                    .Where(x => x.AssemblyName == pluginType.FullName);
            }

            foreach (var metaStep in metaSteps)
            {
                yield return new PluginStepConfig
                {
                    ExecutionStage = (ExecutionStage)metaStep.Stage,
                    EventOperation = metaStep.MessageName,
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
    }
}

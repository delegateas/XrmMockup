using DG.Tools.XrmMockup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmMockupConfig
{
    public class PluginStepConfig
    {
        public PluginStepConfig(StepConfig stepConfig,ExtendedStepConfig extendedStepConfig,IEnumerable<ImageConfig> imageConfigs)
        {
            StepConfig = stepConfig;
            ExtendedStepConfig = extendedStepConfig;
            ImageConfigs = new List<ImageConfig>(imageConfigs);
        }

        public StepConfig StepConfig { get; set; }
        public ExtendedStepConfig ExtendedStepConfig { get; set; }
        public List<ImageConfig> ImageConfigs { get; set; }

        public PluginStepConfig SetExecutionMode(ExecutionMode executionMode)
        {
            ExtendedStepConfig.ExecutionMode = (int)executionMode;
            return this;
        }

        private PluginStepConfig AddFilteredAttribute(string attribute)
        {
            if (string.IsNullOrEmpty(ExtendedStepConfig.FilteredAttributes))
            {
                ExtendedStepConfig.FilteredAttributes = attribute;
            }
            else
            {
                ExtendedStepConfig.FilteredAttributes = ExtendedStepConfig.FilteredAttributes + "," + attribute;
            }
            return this;
        }

        public PluginStepConfig AddFilteredAttributes(params string[] attributes)
        {
            foreach (var attribute in attributes) this.AddFilteredAttribute(attribute);
            return this;
        }

        public PluginStepConfig AddImage(ImageType imageType)
        {
            return this.AddImage(imageType, null);
        }

        public PluginStepConfig AddImage(ImageType imageType, params string[] attributes)
        {
            return this.AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfig AddImage(string name, string entityAlias, ImageType imageType)
        {
            return this.AddImage(name, entityAlias, imageType, null);
        }

        public PluginStepConfig AddImage(string name, string entityAlias, ImageType imageType, string[] attributes)
        {
            ImageConfigs.Add(new ImageConfig(name, entityAlias,(int)imageType, string.Join(",", attributes)));
            return this;
        }

        public PluginStepConfig SetExecutionOrder(int executionOrder)
        {
            ExtendedStepConfig.ExecutionOrder = executionOrder;
            return this;
        }
    }
}

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
            ImageConfigs = ImageConfigs;

        }

        public StepConfig StepConfig { get; set; }
        public ExtendedStepConfig ExtendedStepConfig { get; set; }
        public IEnumerable<ImageConfig> ImageConfigs { get; set; }
    }
}

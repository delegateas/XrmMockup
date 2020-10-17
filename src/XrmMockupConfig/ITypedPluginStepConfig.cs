using System;
using System.Collections.Generic;
using System.Text;
using XrmMockupConfig;

namespace DG.Some.Namespace.Test
{
   public interface ITypedPluginStepConfig
    {
        string _LogicalName { get; }
        string _EventOperation { get; }
        int _ExecutionStage { get; }
        string _Name { get; }
        int _Deployment { get; }
        int _ExecutionMode { get; }
        int _ExecutionOrder { get; }
        string _FilteredAttributes { get; }
        Guid _UserContext { get; }
        IEnumerable<ImageConfig> GetImages();
    }
}

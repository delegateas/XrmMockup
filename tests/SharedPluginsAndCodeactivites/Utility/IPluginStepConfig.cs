namespace DG.Some.Namespace
{
    using System;
    using System.Collections.Generic;

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes, UserContext
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using ImageTuple = System.Tuple<string, string, int, string>;
    #region PluginStepConfig made by Delegate A/S
    interface IPluginStepConfig {
        string _LogicalName { get; }
        string _EventOperation { get; }
        int _ExecutionStage { get; }

        string _Name { get; }
        int _Deployment { get; }
        int _ExecutionMode { get; }
        int _ExecutionOrder { get; }
        string _FilteredAttributes { get; }
        Guid _UserContext { get; }
        IEnumerable<ImageTuple> GetImages();
    }
    #endregion
}
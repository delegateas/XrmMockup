using System;
using System.Collections.Generic;
using System.Text;

namespace MetadataSkeleton.FlowActions
{
    public enum FlowStatus
    {
        Aborted,
        Cancelled,
        Failed,
        Faulted,
        Ignored,
        Paused,
        Running,
        Skipped,
        Succeeded,
        Suspended,
        TimedOut,
        Waiting
    }

    public class GlobalFlowStructures
    {
        public Dictionary<string, object> Parameters;
        public Dictionary<string, object> Outputs;
        public Dictionary<string, object> Variables;
        public Dictionary<string, object> TriggerBody;
    }

    public interface IFlowAction
    {
        FlowStatus Execute(GlobalFlowStructures globalFlowStructures);
    }
}

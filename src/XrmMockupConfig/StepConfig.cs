using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup.Config
{
    public class StepConfig
    {
        public StepConfig(string className, int executionStage, string eventOperation, string logicalName)
        {
            ClassName = className;
            ExecutionStage = executionStage;
            EventOperation = eventOperation.ToLower();
            LogicalName = logicalName;
            if (LogicalName == "none")
                LogicalName = string.Empty;  //shouldnt happend but the metadata generator can spit out "none" as the entity name for associate/disassociate plugins
            
        }
        public string ClassName { get; set; }
        public int ExecutionStage { get; set; }
        public string EventOperation { get; private set; }
        public string LogicalName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmMockupConfig
{
    public class StepConfig
    {
        public StepConfig(string className, int executionStage, string eventOperation, string logicalName)
        {
            ClassName = className;
            ExecutionStage = executionStage;
            EventOperation = eventOperation;
            LogicalName = logicalName;
        }
        public string ClassName { get; set; }
        public int ExecutionStage { get; set; }
        public string EventOperation { get; set; }
        public string LogicalName { get; set; }
    }
}

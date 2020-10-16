using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmMockupConfig
{
    public class ExtendedStepConfig
    {
        public ExtendedStepConfig(int deployment, int executionmode, string name, int executionOrder, string filteredAttributes, Guid? impersonatingUserId)
        {
            Deployment = deployment;
            ExecutionMode = executionmode;
            Name = name;
            ExecutionOrder = executionOrder;
            FilteredAttributes = filteredAttributes;
            ImpersonatingUserId = impersonatingUserId;
        }
        public int Deployment { get; set; }
        public int ExecutionMode { get; set; }
        public string Name { get; set; }
        public int ExecutionOrder { get; set; }
        public string FilteredAttributes { get; set; }
        public Guid? ImpersonatingUserId { get; set; }
    }
}

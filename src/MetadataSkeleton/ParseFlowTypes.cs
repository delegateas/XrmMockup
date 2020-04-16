using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MetadataSkeleton
{
    [DataContract]
    internal class ParseFlow
    {
        [DataMember]
        internal ParseFlowProperties properties;
    }

    [DataContract]
    internal class ParseFlowProperties
    {
        [DataMember]
        internal object connectionReferences;

        [DataMember]
        internal ParseFlowDefinition definition;
    }

    [DataContract]
    internal class ParseFlowDefinition
    {
        [DataMember]
        internal object parameters;

        [DataMember]
        internal Dictionary<string, ParseFlowTrigger> triggers;

        [DataMember]
        internal Dictionary<string, ParseFlowAction> actions;
    }

    [DataContract]
    internal class ParseFlowTrigger
    {
        [DataMember]
        internal string type;

        [DataMember]
        internal string kind;

        [DataMember]
        internal object inputs;
    }

    [DataContract]
    internal class ParseFlowAction
    {
        [DataMember]
        internal Dictionary<string,string[]> runAfter;

        [DataMember]
        internal string type;

        [DataMember]
        internal string kind;

        [DataMember]
        internal object inputs;

        [DataMember(Name = "foreach")]
        internal string _foreach;

        [DataMember]
        internal Dictionary<string,ParseFlowAction> actions;
    }
}

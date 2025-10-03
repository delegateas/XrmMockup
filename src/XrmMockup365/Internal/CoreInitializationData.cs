using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Internal
{
    /// <summary>
    /// Data transfer object for core initialization
    /// </summary>
    internal class CoreInitializationData
    {
        public XrmMockupSettings Settings { get; set; }
        public MetadataSkeleton Metadata { get; set; }
        public List<Entity> Workflows { get; set; }
        public List<SecurityRole> SecurityRoles { get; set; }
        public EntityReference BaseCurrency { get; set; }
        public int BaseCurrencyPrecision { get; set; }
        public OrganizationServiceProxy OnlineProxy { get; set; }
        public Dictionary<string, Type> EntityTypeMap { get; set; }
    }
}

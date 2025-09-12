using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup
{
    public class StaticMetadataCache
    {
        public MetadataSkeleton Metadata { get; }
        public List<Entity> Workflows { get; }
        public List<SecurityRole> SecurityRoles { get; }
        public Dictionary<string, Type> EntityTypeMap { get; }
        public EntityReference BaseCurrency { get; }
        public int BaseCurrencyPrecision { get; }
        public OrganizationServiceProxy OnlineProxy { get; }

        public StaticMetadataCache(MetadataSkeleton metadata, List<Entity> workflows, List<SecurityRole> securityRoles,
            Dictionary<string, Type> entityTypeMap, EntityReference baseCurrency, int baseCurrencyPrecision, 
            OrganizationServiceProxy onlineProxy)
        {
            Metadata = metadata;
            Workflows = workflows;
            SecurityRoles = securityRoles;
            EntityTypeMap = entityTypeMap;
            BaseCurrency = baseCurrency;
            BaseCurrencyPrecision = baseCurrencyPrecision;
            OnlineProxy = onlineProxy;
        }
    }
}

using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
#if DATAVERSE_SERVICE_CLIENT
using DG.Tools.XrmMockup.Online;
#endif

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
#if DATAVERSE_SERVICE_CLIENT
        internal IOnlineDataService OnlineDataService { get; }

        internal StaticMetadataCache(MetadataSkeleton metadata, List<Entity> workflows, List<SecurityRole> securityRoles,
            Dictionary<string, Type> entityTypeMap, EntityReference baseCurrency, int baseCurrencyPrecision,
            IOnlineDataService onlineDataService)
        {
            Metadata = metadata;
            Workflows = workflows;
            SecurityRoles = securityRoles;
            EntityTypeMap = entityTypeMap;
            BaseCurrency = baseCurrency;
            BaseCurrencyPrecision = baseCurrencyPrecision;
            OnlineDataService = onlineDataService;
        }
#else
        internal StaticMetadataCache(MetadataSkeleton metadata, List<Entity> workflows, List<SecurityRole> securityRoles,
            Dictionary<string, Type> entityTypeMap, EntityReference baseCurrency, int baseCurrencyPrecision)
        {
            Metadata = metadata;
            Workflows = workflows;
            SecurityRoles = securityRoles;
            EntityTypeMap = entityTypeMap;
            BaseCurrency = baseCurrency;
            BaseCurrencyPrecision = baseCurrencyPrecision;
        }
#endif
    }
}

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup {

    public class MetadataSkeleton {
        public Dictionary<string, EntityMetadata> EntityMetadata;
        public List<Entity> Currencies;
        public Entity BaseOrganization;
        public Entity RootBusinessUnit;
        public List<MetaPlugin> Plugins;
        public OptionSetMetadataBase[] OptionSets;
        public Dictionary<string, Dictionary<int,int>> DefaultStateStatus;
        public List<Entity> AccessTeamTemplates;

        public void Merge(MetadataSkeleton metadata)
        {
            foreach (var kvp in metadata.EntityMetadata)
            {
                if (!this.EntityMetadata.ContainsKey(kvp.Key))
                {
                    this.EntityMetadata.Add(kvp.Key, kvp.Value);
                }
            }
        }

    }

    public class MetaPlugin {
        public string FilteredAttributes;
        public int Mode;
        public string Name;
        public int Rank;
        public int Stage;
        public string MessageName;
        public string AssemblyName;
        public string PrimaryEntity;
        public List<MetaImage> Images;
    }

    public class MetaImage
    {
        public string Attributes;
        public string EntityAlias;
        public string Name;
        public int ImageType;
    }


    public class RolePrivilege {
        public bool CanBeGlobal;
        public bool CanBeDeep;
        public bool CanBeLocal;
        public bool CanBeBasic;
        public AccessRights AccessRight;
        public PrivilegeDepth PrivilegeDepth;

        public RolePrivilege Clone()
        {
            var clone = new RolePrivilege();
            clone.CanBeGlobal = this.CanBeGlobal;
            clone.CanBeDeep = this.CanBeDeep;
            clone.CanBeLocal = this.CanBeLocal;
            clone.CanBeLocal = this.CanBeLocal;
            clone.AccessRight = this.AccessRight;
            clone.PrivilegeDepth = this.PrivilegeDepth;

            return clone;
        }
    }

    public class SecurityRole {
        public Dictionary<string, Dictionary<AccessRights, RolePrivilege>> Privileges;
        public string Name;
        public EntityReference BusinessUnitId;
        public Guid RoleId;
        public Guid RoleTemplateId;

        public SecurityRole Clone()
        {
            var clone = new SecurityRole();
            clone.Privileges = this.Privileges;
            clone.Name = "Clone of " + this.Name;
            clone.BusinessUnitId = this.BusinessUnitId;
            clone.RoleId = Guid.NewGuid();
            clone.RoleTemplateId = this.RoleTemplateId;

            return clone;
        }

    }
}

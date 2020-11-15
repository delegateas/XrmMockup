#if !(XRM_MOCKUP_2011)
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class RemoveUserFromRecordTeamRequestHandler : RequestHandler
    {
        internal RemoveUserFromRecordTeamRequestHandler(Core core, IXrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RemoveUserFromRecordTeam") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            //validate that the team template exists
            var ttId = (Guid)orgRequest["TeamTemplateId"];

            var record = orgRequest["Record"] as EntityReference;

            var accessTeam = security.GetAccessTeam(ttId, record.Id);

            var membershiprow = security.GetTeamMembership(accessTeam.Id, (Guid)orgRequest["SystemUserId"]);
            db.Delete(membershiprow);

            if (membershiprow != null)
            {
                var poa = security.GetPOA((Guid)orgRequest["SystemUserId"], record.Id);

                if (poa != null)
                {
                    //we need t update the poa record with the access masks from the the access teams the user is left in
                    //get the users remaining team memberships
                    var remainingAccessTeams = security.GetAccessTeams(record.Id);
                    int mask = 0;
                    foreach (var remainingAccessTeam in remainingAccessTeams)
                    {
                        var ttRow = core.GetEntity(new EntityReference("teamtemplate", remainingAccessTeam.GetAttributeValue<EntityReference>("teamtemplateid").Id));
                        var remainingTeamMembership = security.GetTeamMembership(remainingAccessTeam.Id, (Guid)orgRequest["SystemUserId"]);
                        if (remainingTeamMembership != null)
                        {
                            mask = mask | ttRow.GetAttributeValue<int>("defaultaccessrightsmask");
                        }
                    }
                    security.OverwritePOAMask(poa.Id, mask);
                }
            }

            var resp = new RemoveUserFromRecordTeamResponse();
            resp.Results["AccessTeamId"] = accessTeam.Id;
            return resp;
        }
    }
}
#endif
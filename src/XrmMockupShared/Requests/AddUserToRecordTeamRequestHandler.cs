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
    internal class AddUserToRecordTeamRequestHandler : RequestHandler
    {
        internal AddUserToRecordTeamRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "AddUserToRecordTeam") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {

            //validate that the team template exists
            var ttId = (Guid)orgRequest["TeamTemplateId"];
            var ttRow = core.GetDbRow(new EntityReference("teamtemplate", ttId)).ToEntity() ;

            var record = orgRequest["Record"] as EntityReference;

            var accessTeam = security.GetAccessTeam(ttId, record.Id);
            if (accessTeam == null)
            {
                accessTeam = security.AddAccessTeam(ttId, record);
            }


            var membershiprow = security.GetTeamMembership(accessTeam.Id, (Guid)orgRequest["SystemUserId"]);
            if (membershiprow==null)
            {
                membershiprow = security.AddTeamMembership(accessTeam.Id, (Guid)orgRequest["SystemUserId"]);

                var poa = security.GetPOA((Guid)orgRequest["SystemUserId"], record.Id);

                if (poa == null)
                {
                    poa = security.AddPOA((Guid)orgRequest["SystemUserId"], record, ttRow.GetAttributeValue<int>("defaultaccessrightsmask"));
                }
            }

            var resp = new AddUserToRecordTeamResponse();
            resp.Results["AccessTeamId"] = accessTeam.Id;
            return resp;
        }
    }
}
#endif

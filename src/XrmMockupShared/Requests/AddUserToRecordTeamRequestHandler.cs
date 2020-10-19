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
            var ttRow = core.GetDbRow(new EntityReference("teamtemplate", ttId));

            var rows = db.GetDBEntityRows("team").Where(x => (int)x.GetColumn("teamtype") == 1)
                                             .Where(x => (x.GetColumn("teamtemplateid") as DbRow).Id == ttId)
                                             .Where(x => (x.GetColumn("regardingobjectid") as DbRow).Id == (orgRequest["Record"] as EntityReference).Id);
            Guid teamId;
            if (rows.Any())
            {
                teamId = rows.Single().Id;
            }
            else
            {
                //create the team
                teamId = Guid.NewGuid();
                var team = new Entity("team");
                team.Id = teamId;
                team["teamtype"] = new OptionSetValue(1);
                team["teamtemplateid"] = new EntityReference("teamtemplate", ttId);
                team["regardingobjectid"] = orgRequest["Record"];
                team["name"] = (orgRequest["Record"] as EntityReference).Id.ToString() + "+" + ttId.ToString();
                db.Add(team);
            }


            var membershiprows = db.GetDBEntityRows("teammembership").Where(x => ((Guid)x.GetColumn("teamid") == teamId))
                                                                     .Where(x => ((Guid)x.GetColumn("systemuserid") == (Guid)orgRequest["SystemUserId"]));
            if (!membershiprows.Any())
            {
                //create the team
                var teammembership = new Entity("teammembership");
                teammembership["teamid"] = teamId;
                teammembership["systemuserid"] = (Guid)orgRequest["SystemUserId"];
                db.Add(teammembership);

            }





            //var request = MakeRequest<AddMembersTeamRequest>(orgRequest);

            //// Check if the team exist
            //if (!db.HasRow(new EntityReference(LogicalNames.Team, request.TeamId)))
            //{
            //    throw new MockupException($"Team with id {request.TeamId} does not exist");
            //}

            //var teamMembers = db.GetDBEntityRows(LogicalNames.TeamMembership).Select(x => x.ToEntity()).Where(x => x.GetAttributeValue<Guid>("teamid") == request.TeamId);

            //foreach (var userId in request.MemberIds)
            //{
            //    // Check if the user exist
            //    if (!db.HasRow(new EntityReference(LogicalNames.SystemUser, userId)))
            //    {
            //        throw new MockupException($"User with id {userId} does not exist");
            //    }

            //    // Check if the user is already a member of the team
            //    if (teamMembers.Any(t => t.GetAttributeValue<Guid>("systemuserid") == userId))
            //    {
            //        throw new MockupException($"User with id {userId} is already member of the team with id {request.TeamId}");
            //    }
            //}

            //foreach (var userId in request.MemberIds)
            //{
            //    var teamMember = new Entity(LogicalNames.TeamMembership);
            //    teamMember["teamid"] = request.TeamId;
            //    teamMember["systemuserid"] = userId;
            //    db.Add(teamMember);
            //}

            return new AddUserToRecordTeamResponse();
        }
    }
}
#endif

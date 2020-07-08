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
            var request = MakeRequest<AddUserToRecordTeamRequest>(orgRequest);
            var resp = new AddUserToRecordTeamResponse();
            var settings = MockupExecutionContext.GetSettings(request);

            //check to see if the access team exists
            //where regarding object id = request.record.id
            //and teamtemplateid = request.teamtemplated

            var currentTeams = db.GetDBEntityRows("team");

            currentTeams = currentTeams.Where(x => x.GetColumn("regardingobjectid") != null);
            currentTeams = currentTeams.Where(x => (x.ToEntity().GetAttributeValue<EntityReference>("regardingobjectid").Id == request.Record.Id));
            currentTeams = currentTeams.Where(x => x.GetColumn("teamtemplateid") != null);
            currentTeams = currentTeams.Where(x => (x.ToEntity().GetAttributeValue<EntityReference>("teamtemplateid").Id == request.TeamTemplateId));

            Entity team;

            if (!currentTeams.Any())
            {
                team = new Entity("team");
                team.Id = Guid.NewGuid();
                team["regardingobjectid"] = request.Record;
                team["teamtemplateid"] = new EntityReference("teamtemplate", request.TeamTemplateId);
                team["teamtype"] = new OptionSetValue(1); //access team
                db.Add(team);
                resp.Results["AccessTeamId"] = team.Id;
            }
            else
            {
                team = currentTeams.Single().ToEntity();
            }

            //and check to see if the user is a member of it
            var currentTeamMembers = db.GetDBEntityRows("teammembership");
            currentTeamMembers = currentTeamMembers.Where(x => x.GetColumn("teamid") != null);
            currentTeamMembers = currentTeamMembers.Where(x => (x.ToEntity().GetAttributeValue<Guid>("teamid") == team.Id));
            currentTeamMembers = currentTeamMembers.Where(x => x.GetColumn("systemuserid") != null);
            currentTeamMembers = currentTeamMembers.Where(x => (x.ToEntity().GetAttributeValue<Guid>("systemuserid") == request.SystemUserId));

            if (!currentTeamMembers.Any())
            {
                var teamMember = new Entity("teammembership");
                teamMember["teamid"] = team.Id;
                teamMember["systemuserid"] = request.SystemUserId;
                db.Add(teamMember);
            }

            

            return resp;
        }
    }
}

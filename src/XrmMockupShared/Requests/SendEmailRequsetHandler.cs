using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class SendEmailRequsetHandler : RequestHandler
    {
        const int EMAIL_STATE_COMPLETED = 1;
        const int EMAIL_STATUS_DRAFT = 1;
        const int EMAIL_STATUS_PENDING_SEND = 6;
        const int EMAIL_STATUS_SENT = 3;

        public SendEmailRequsetHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SendEmail") { }

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailRequest>(orgRequest);

            var emailRef = new EntityReference("email", request.EmailId);

            if (!security.HasPermission(emailRef, AccessRights.ReadAccess, userRef))
            {
                throw new FaultException($"Principal user (Id={userRef.Id}) is missing Read privilege for Email (Id={request.EmailId})");
            }

            if (!security.HasPermission(emailRef, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException($"Principal user (Id={userRef.Id}) is missing Write privilege for Email (Id={request.EmailId})");
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailRequest>(orgRequest);

            if (request.EmailId == Guid.Empty)
            {
               throw new FaultException("Required field 'EmailId' is missing");
            }

            var emailRef = new EntityReference("email", request.EmailId);

            var email = db.GetEntityOrNull(emailRef);
            if (email is null)
            {
                throw new FaultException($"Email with Id = {request.EmailId} does not exist");
            }

            if (email.GetAttributeValue<bool>("directioncode") is false)
            {
                throw new FaultException("Cannot send incoming email messages");
            }

            if (email.GetAttributeValue<OptionSetValue>("statuscode").Value != EMAIL_STATUS_DRAFT)
            {
                throw new FaultException("Email must be in Draft status to send");
            }

            if (email.Contains("from"))
            {
                var from = email.GetAttributeValue<EntityCollection>("from");
                if (from.Entities.Count != 1)
                {
                    throw new FaultException("Email must have one and only one sender");
                }

                var activityParty = from.Entities[0];
                if (activityParty.Contains("partyid"))
                {
                    var partyRef = activityParty.GetAttributeValue<EntityReference>("partyid");
                    if (db.GetEntityOrNull(partyRef) is null)
                    {
                        throw new FaultException($"{partyRef.LogicalName} with Id = {partyRef.Id} does not exist");
                    }
                }
                else
                {
                    throw new FaultException("Sender must be an existing Entity");
                }
            }
            else
            {
                throw new FaultException("Email must have a sender");
            }

            if (email.Contains("to"))
            {
                var to = email.GetAttributeValue<EntityCollection>("to");
                if (to.Entities.Count is 0)
                {
                    throw new FaultException("Email must have at least one recipient to send");
                }

                foreach (Entity activityParty in to.Entities)
                {
                    if (activityParty.Contains("partyid"))
                    {
                        var partyRef = activityParty.GetAttributeValue<EntityReference>("partyid");
                        if (db.GetEntityOrNull(partyRef) is null)
                        {
                            throw new FaultException($"{partyRef.LogicalName} with Id = {partyRef.Id} does not exist");
                        }
                    }
                    else if (!activityParty.Contains("addressused"))
                    {
                        throw new FaultException("Invalid ActivityParty");
                    }
                }
            }
            else
            {
                throw new FaultException("Email must have recipients to send");
            }

            db.Update(new Entity("email")
            {
                Id = request.EmailId,
                ["statecode"] = new OptionSetValue(EMAIL_STATE_COMPLETED),
                ["statuscode"] = new OptionSetValue(request.IssueSend ? EMAIL_STATUS_PENDING_SEND : EMAIL_STATUS_SENT)
            });

            return new SendEmailResponse();
        }
    }
}

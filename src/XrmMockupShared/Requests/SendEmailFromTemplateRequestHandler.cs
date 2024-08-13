using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class SendEmailFromTemplateRequestHandler : RequestHandler
    {
        private SendEmailRequestHandler sendEmailRequestHandler;
        private const int EMAIL_STATE_COMPLETED = 1;
        private const int EMAIL_STATUS_DRAFT = 1;
        private const int EMAIL_STATUS_PENDING_SEND = 6;

        public SendEmailFromTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SendEmailFromTemplate") 
        {
            sendEmailRequestHandler = new SendEmailRequestHandler(core, db, metadata, security);
        }

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailFromTemplateRequest>(orgRequest);
            if (request.Target is null) return;

            var emailRef = new EntityReference("email", request.Target.Id);

            sendEmailRequestHandler.ValidateEmailSecurity(emailRef, userRef);
            
            if (request.TemplateId == Guid.Empty) return;

            var templateRef = new EntityReference("template", request.TemplateId);

            if (!security.HasPermission(templateRef, AccessRights.ReadAccess, userRef))
            {
                throw new FaultException($"Principal user (Id={userRef.Id}) is missing Read privilege for Template (Id={request.TemplateId})");
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailFromTemplateRequest>(orgRequest);

            if (request.RegardingId == Guid.Empty)
            {
                throw new FaultException("Required field 'RegardingId' is missing");
            }

            if (request.RegardingType is null)
            {
                throw new FaultException("Required field 'RegardingType' is missing");
            }

            var regardingRef = new EntityReference(request.RegardingType, request.RegardingId);
            if (db.GetEntityOrNull(regardingRef) is null)
            {
                throw new FaultException($"{request.RegardingType} with Id = {request.RegardingId} does not exist");
            }

            #region Validate Target

            if (request.Target is null)
            {
                throw new FaultException("Required field 'Target' is missing");
            }

            var emailRef = new EntityReference("email", request.Target.Id);

            var email = db.GetEntityOrNull(emailRef);
            if (email is null)
            {
                throw new FaultException($"email with Id = {request.Target.Id} does not exist");
            }

            sendEmailRequestHandler.ValidateEmail(email);

            if (email.Contains("regardingobjectid"))
            {
                var regardingObjectRef = email.GetAttributeValue<EntityReference>("regardingobjectid");
                if (db.GetEntityOrNull(regardingObjectRef) is null)
                {
                    throw new FaultException($"{regardingObjectRef.LogicalName} with Id = {regardingObjectRef.Id} does not exist");
                }

                if (regardingObjectRef.Id != request.RegardingId)
                {
                    throw new FaultException($"RegardingObjectId (Id={regardingObjectRef.Id}) is different from RegardingId (Id={request.RegardingId})");
                }
            }
            else
            {
                throw new FaultException("Email must have a regarding object");
            }

            #endregion

            #region Validate TemplateId

            if (request.TemplateId == Guid.Empty)
            {
                throw new FaultException("Required field 'TemplateId' is missing");
            }

            var templateRef = new EntityReference("template", request.TemplateId);

            var template = db.GetEntityOrNull(templateRef);
            if (template is null)
            {
                throw new FaultException($"template with Id = {request.TemplateId} does not exist");
            }

            if (template.Contains("templatetypecode"))
            {
                var templateTypeCode = template.GetAttributeValue<string>("templatetypecode");
                if (templateTypeCode != request.RegardingType)
                {
                    throw new FaultException($"template with Id = {request.TemplateId} is not of type {request.RegardingType}");
                }
            }
            else
            {
                throw new FaultException("Template must have a Template Type Code");
            }

            #endregion

            db.Update(new Entity("email")
            {
                Id = request.Target.Id,
                ["subject"] = template.GetAttributeValue<string>("subject"),
                ["description"] = template.GetAttributeValue<string>("body"),
                ["statecode"] = new OptionSetValue(EMAIL_STATE_COMPLETED),
                ["statuscode"] = new OptionSetValue(EMAIL_STATUS_PENDING_SEND)
            });

            return new SendEmailFromTemplateResponse();
        }
    }
}

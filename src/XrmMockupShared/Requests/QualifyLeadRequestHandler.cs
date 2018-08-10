using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class QualifyLeadRequestHandler : RequestHandler
    {
        internal QualifyLeadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "QualifyLead")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<QualifyLeadRequest>(orgRequest);

            if (request.LeadId == null)
            {
                throw new FaultException("Required field 'LeadId' is missing");
            }

            var leadMetadata = metadata.EntityMetadata.GetMetadata(request.LeadId.LogicalName);

            if (leadMetadata == null)
            {
                throw new FaultException($"The entity with a name = '{request.LeadId.LogicalName}' was not found in the MetadataCache.");
            }

            if (request.OpportunityCurrencyId != null)
            {
                var currencyMetadata = metadata.EntityMetadata.GetMetadata(request.OpportunityCurrencyId.LogicalName);
                if (currencyMetadata == null)
                {
                    throw new FaultException($"The entity with a name = '{request.OpportunityCurrencyId.LogicalName}' was not found in the MetadataCache.");
                }
            }

            EntityMetadata customerMetadata = null;
            if (request.OpportunityCustomerId != null)
            {
                customerMetadata = metadata.EntityMetadata.GetMetadata(request.OpportunityCustomerId.LogicalName);
                if (customerMetadata == null)
                {
                    throw new FaultException($"The entity with a name = '{request.OpportunityCustomerId.LogicalName}' was not found in the MetadataCache.");
                }
            }

            var leadRow = db.GetDbRow(request.LeadId);

            if (leadRow == null || request.LeadId.LogicalName != LogicalNames.Lead)
            {
                throw new FaultException($"{LogicalNames.Lead} With Id = {request.LeadId.Id} Does Not Exist");
            }

            var currentState = leadRow.GetColumn<int>("statecode");

            if (currentState == 1)
            {
                throw new FaultException($"The {LogicalNames.Lead} is already closed.");
            }

            var status = request.Status != null ? request.Status.Value : 0;
            var statusOptionMetadata = Utility.GetStatusOptionMetadata(leadMetadata)
                .FirstOrDefault(s => s.Value == status) as StatusOptionMetadata;

            if (statusOptionMetadata == null)
            {
                throw new FaultException($"{status} is not a valid status code on lead with Id {request.LeadId.Id}");
            }

            if (statusOptionMetadata.State != 1)
            {
                throw new FaultException($"{request.Status.Value} is not a valid status code for state code LeadState.Qualified lead with Id {request.LeadId.Id}");
            }

            var createdEntities = new EntityReferenceCollection();

            var lead = leadRow.ToEntity();

            if (request.CreateAccount)
            {
                createdEntities.Add(CreateAccountFromLead(lead, userRef));
            }

            if (request.CreateContact)
            {
                createdEntities.Add(CreateContactFromLead(lead, userRef));
            }

            if (request.CreateOpportunity)
            {
                createdEntities.Add(CreateOpportunityFromLead(lead, userRef, request.OpportunityCustomerId, request.OpportunityCurrencyId));
            }

            lead["statuscode"] = status;
            db.Update(lead);

            var resp = new QualifyLeadResponse();
            resp.Results["CreatedEntities"] = createdEntities;
            return resp;
        }

        private EntityReference CreateOpportunityFromLead(Entity lead, EntityReference userRef, EntityReference customer, EntityReference currency)
        {
            if (customer != null)
            {
                if (customer.LogicalName != LogicalNames.Account &&
                    customer.LogicalName != LogicalNames.Contact)
                {
                    throw new FaultException($"CustomerIdType for {LogicalNames.Opportunity} can either be an {LogicalNames.Account} or {LogicalNames.Contact}");
                }
            }

            var opportunity = new Entity(LogicalNames.Opportunity);
            if (lead.Attributes.Contains("subject"))
                opportunity["name"] = lead["subject"];
            if (lead.Attributes.Contains("qualificationcomments"))
                opportunity["qualificationcomments"] = lead["qualificationcomments"];
            if (lead.Attributes.Contains("description"))
                opportunity["description"] = lead["description"];
            opportunity["originatingleadid"] = lead.ToEntityReference();

            if(customer != null)
            {
                opportunity["customerid"] = customer;
            }

            if(currency != null)
            {
                opportunity["transactioncurrencyid"] = currency;
            }

            opportunity.Id = CreateEntity(opportunity, userRef);
            return opportunity.ToEntityReference();
        }

        private EntityReference CreateContactFromLead(Entity lead, EntityReference userRef)
        {
            var contact = new Entity(LogicalNames.Contact);
            if (lead.Attributes.Contains("mobilephone"))
                contact["mobilephone"] = lead["mobilephone"];
            if (lead.Attributes.Contains("emailaddress1"))
                contact["emailaddress1"] = lead["emailaddress1"];
            if (lead.Attributes.Contains("emailaddress3"))
                contact["emailaddress3"] = lead["emailaddress3"];
            if (lead.Attributes.Contains("websiteurl"))
                contact["websiteurl"] = lead["websiteurl"];
            if (lead.Attributes.Contains("yomilastname"))
                contact["yomilastname"] = lead["yomilastname"];
            if (lead.Attributes.Contains("lastname"))
                contact["lastname"] = lead["lastname"];
            if (lead.Attributes.Contains("donotpostalmail"))
                contact["donotpostalmail"] = lead["donotpostalmail"];
            if (lead.Attributes.Contains("donotphone"))
                contact["donotphone"] = lead["donotphone"];
            if (lead.Attributes.Contains("yomimiddlename"))
                contact["yomimiddlename"] = lead["yomimiddlename"];
            if (lead.Attributes.Contains("description"))
                contact["description"] = lead["description"];
            if (lead.Attributes.Contains("firstname"))
                contact["firstname"] = lead["firstname"];
            if (lead.Attributes.Contains("donotemail"))
                contact["donotemail"] = lead["donotemail"];
            if (lead.Attributes.Contains("address1_stateorprovince"))
                contact["address1_stateorprovince"] = lead["address1_stateorprovince"];
            if (lead.Attributes.Contains("address2_county"))
                contact["address2_county"] = lead["address2_county"];
            if (lead.Attributes.Contains("donotfax"))
                contact["donotfax"] = lead["donotfax"];
            if (lead.Attributes.Contains("donotsendmm"))
                contact["donotsendmm"] = lead["donotsendmm"];
            if (lead.Attributes.Contains("jobtitle"))
                contact["jobtitle"] = lead["jobtitle"];
            if (lead.Attributes.Contains("pager"))
                contact["pager"] = lead["pager"];
            if (lead.Attributes.Contains("address1_country"))
                contact["address1_country"] = lead["address1_country"];
            if (lead.Attributes.Contains("address1_line1"))
                contact["address1_line1"] = lead["address1_line1"];
            if (lead.Attributes.Contains("address1_line2"))
                contact["address1_line2"] = lead["address1_line2"];
            if (lead.Attributes.Contains("address1_line3"))
                contact["address1_line3"] = lead["address1_line3"];
            if (lead.Attributes.Contains("telephone2"))
                contact["telephone2"] = lead["telephone2"];
            if (lead.Attributes.Contains("telephone3"))
                contact["telephone3"] = lead["telephone3"];
            if (lead.Attributes.Contains("address2_fax"))
                contact["address2_fax"] = lead["address2_fax"];
            if (lead.Attributes.Contains("donotbulkemail"))
                contact["donotbulkemail"] = lead["donotbulkemail"];
            if (lead.Attributes.Contains("emailaddress2"))
                contact["emailaddress2"] = lead["emailaddress2"];
            if (lead.Attributes.Contains("fax"))
                contact["fax"] = lead["fax"];
            if (lead.Attributes.Contains("yomifirstname"))
                contact["yomifirstname"] = lead["yomifirstname"];
            if (lead.Attributes.Contains("address1_postalcode"))
                contact["address1_postalcode"] = lead["address1_postalcode"];
            if (lead.Attributes.Contains("address1_city"))
                contact["address1_city"] = lead["address1_city"];
            if (lead.Attributes.Contains("address2_country"))
                contact["address2_country"] = lead["address2_country"];
            contact["originatingleadid"] = lead.ToEntityReference();

            contact.Id = CreateEntity(contact, userRef);
            return contact.ToEntityReference();
        }

        private EntityReference CreateAccountFromLead(Entity lead, EntityReference userRef)
        {
            var account = new Entity(LogicalNames.Account);

            if(lead.Attributes.Contains("sic"))
                account["sic"] = lead["sic"];
            if(lead.Attributes.Contains("emailaddress1"))
                account["emailaddress1"] = lead["emailaddress1"];
            if(lead.Attributes.Contains("companyname"))
                account["name"] = lead["companyname"];
            if (lead.Attributes.Contains("fax"))
                account["fax"] = lead["fax"];
            if (lead.Attributes.Contains("websiteurl"))
                account["websiteurl"] = lead["websiteurl"];
            if (lead.Attributes.Contains("address1_country"))
                account["address1_country"] = lead["address1_country"];
            if (lead.Attributes.Contains("address1_city"))
                account["address1_city"] = lead["address1_city"];
            if (lead.Attributes.Contains("address1_line1"))
                account["address1_line1"] = lead["address1_line1"];
            if (lead.Attributes.Contains("address1_line2"))
                account["address1_line2"] = lead["address1_line2"];
            if (lead.Attributes.Contains("address1_line3"))
                account["address1_line3"] = lead["address1_line3"];
            if (lead.Attributes.Contains("address1_postalcode"))
                account["address1_postalcode"] = lead["address1_postalcode"];
            if (lead.Attributes.Contains("address1_stateorprovince"))
                account["address1_stateorprovince"] = lead["address1_stateorprovince"];
            if (lead.Attributes.Contains("telephone2"))
                account["telephone2"] = lead["telephone2"];
            if (lead.Attributes.Contains("donotpostalmail"))
                account["donotpostalmail"] = lead["donotpostalmail"];
            if (lead.Attributes.Contains("donotphone"))
                account["donotphone"] = lead["donotphone"];
            if (lead.Attributes.Contains("donotfax"))
                account["donotfax"] = lead["donotfax"];
            if (lead.Attributes.Contains("donotsendmm"))
                account["donotsendmm"] = lead["donotsendmm"];
            if (lead.Attributes.Contains("description"))
                account["description"] = lead["description"];
            if (lead.Attributes.Contains("donotemail"))
                account["donotemail"] = lead["donotemail"];
            if (lead.Attributes.Contains("yominame"))
                account["yominame"] = lead["yominame"];
            if (lead.Attributes.Contains("donotbulkemail"))
                account["donotbulkemail"] = lead["donotbulkemail"];
            account["originatingleadid"] = lead.ToEntityReference();

            account.Id = CreateEntity(account, userRef);
            return account.ToEntityReference();
        }

        private Guid CreateEntity(Entity entity, EntityReference userRef)
        {
            var req = new CreateRequest
            {
                Target = entity
            };
            var response = core.Execute(req, userRef) as CreateResponse;
            return response.id;
        }
    }
}
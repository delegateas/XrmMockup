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
        private readonly Dictionary<string, string> leadToAccountAttributesMap = new Dictionary<string, string>
        {
            { "sic", "sic" },
            { "emailaddress1", "emailaddress1" },
            { "companyname", "name" },
            { "fax", "fax" },
            { "websiteurl", "websiteurl" },
            { "address1_country", "address1_country" },
            { "address1_city", "address1_city" },
            { "address1_line1", "address1_line1" },
            { "address1_line2", "address1_line2" },
            { "address1_line3", "address1_line3" },
            { "address1_postalcode", "address1_postalcode" },
            { "address1_stateorprovince", "address1_stateorprovince" },
            { "telephone2", "telephone2" },
            { "donotpostalmail", "donotpostalmail" },
            { "donotphone", "donotphone" },
            { "donotfax", "donotfax" },
            { "donotsendmm", "donotsendmm" },
            { "description", "description" },
            { "donotemail", "donotemail" },
            { "yomicompanyname", "yominame" },
            { "donotbulkemail", "donotbulkemail" }
        };

        private readonly Dictionary<string, string> leadToContactAttributesMap = new Dictionary<string, string>
        {
            { "mobilephone", "mobilephone" },
            { "emailaddress1", "emailaddress1" },
            { "emailaddress3", "emailaddress3" },
            { "websiteurl", "websiteurl" },
            { "yomilastname", "yomilastname" },
            { "lastname", "lastname" },
            { "donotpostalmail", "donotpostalmail" },
            { "donotphone", "donotphone" },
            { "yomimiddlename", "yomimiddlename" },
            { "description", "description" },
            { "firstname", "firstname" },
            { "donotemail", "donotemail" },
            { "address1_stateorprovince", "address1_stateorprovince" },
            { "address2_county", "address2_county" },
            { "donotfax", "donotfax" },
            { "donotsendmm", "donotsendmm" },
            { "jobtitle", "jobtitle" },
            { "pager", "pager" },
            { "address1_country", "address1_country" },
            { "address1_line1", "address1_line1" },
            { "address1_line2", "address1_line2" },
            { "address1_line3", "address1_line3" },
            { "telephone2", "telephone2" },
            { "telephone3", "telephone3" },
            { "address2_fax", "address2_fax" },
            { "donotbulkemail", "donotbulkemail" },
            { "emailaddress2", "emailaddress2" },
            { "fax", "fax" },
            { "yomifirstname", "yomifirstname" },
            { "address1_postalcode", "address1_postalcode" },
            { "address1_city", "address1_city" },
            { "address2_country", "address2_country" }
        };

        private readonly Dictionary<string, string> leadToOpportunityAttributesMap = new Dictionary<string, string>
        {
            { "subject", "name" },
            { "qualificationcomments", "qualificationcomments" },
            { "description", "description" }
        };

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
                createdEntities.Add(CreateEntityFromLead(lead, LogicalNames.Account, leadToAccountAttributesMap, userRef).ToEntityReference());
            }

            if (request.CreateContact)
            {
                createdEntities.Add(CreateEntityFromLead(lead, LogicalNames.Contact, leadToContactAttributesMap, userRef).ToEntityReference());
            }

            if (request.CreateOpportunity)
            {
                createdEntities.Add(CreateOpportunityFromLead(lead, request.OpportunityCustomerId, request.OpportunityCurrencyId, userRef).ToEntityReference());
            }

            lead["statuscode"] = status;
            db.Update(lead);

            var resp = new QualifyLeadResponse();
            resp.Results["CreatedEntities"] = createdEntities;
            return resp;
        }

        private Entity CreateOpportunityFromLead(Entity lead, EntityReference customer, EntityReference currency, EntityReference userRef)
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
            MapAttributesFromLead(lead, leadToOpportunityAttributesMap, ref opportunity);
            opportunity["originatingleadid"] = lead.ToEntityReference();

            if (customer != null)
            {
                opportunity["customerid"] = customer;
            }

            if(currency != null)
            {
                opportunity["transactioncurrencyid"] = currency;
            }

            opportunity.Id = CreateEntity(opportunity, userRef);

            return opportunity;
        }

        private Entity CreateEntityFromLead(Entity lead, string newEntityLogicalName, IDictionary<string, string> attributesMap, EntityReference userRef)
        {
            var newEntity = new Entity(newEntityLogicalName);

            MapAttributesFromLead(lead, attributesMap, ref newEntity);

            newEntity["originatingleadid"] = lead.ToEntityReference();

            newEntity.Id = CreateEntity(newEntity, userRef);
            return newEntity;
        }

        private void MapAttributesFromLead(Entity lead, IDictionary<string, string> attributesMap, ref Entity toEntity)
        {
            foreach(var attr in attributesMap)
            {
                if(lead.Attributes.Contains(attr.Key))
                    toEntity[attr.Value] = lead[attr.Key];
            }
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
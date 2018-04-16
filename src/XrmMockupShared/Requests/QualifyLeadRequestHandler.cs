using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

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

            var lead = db.GetDbRow(request.LeadId);

            if (lead == null || request.LeadId.LogicalName != "lead")
            {
                throw new FaultException($"lead With Id = {request.LeadId.Id} Does Not Exist");
            }

            var currentState = lead.GetColumn<int>("statecode");

            if (currentState == 1)
            {
                throw new FaultException("The lead is already closed.");
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

            if (request.CreateAccount)
            {
                var accountAlreadyExists = db["account"].Any(row => row["name"] != null && row["name"].Equals(lead["fullname"]));

                if (accountAlreadyExists)
                {
                    throw new FaultException("A record was not created or updated because a duplicate of the current record already exists.");
                }

                var account = new Entity
                {
                    Id = Guid.NewGuid(),
                    LogicalName = "account",
                };

                createdEntities.Add(account.ToEntityReference());
            }

            if (request.CreateContact)
            {
                var contactAlreadyExists = db["contact"].Any(row => row["fullname"] != null && row["fullname"].Equals(lead["fullname"]));

                if (contactAlreadyExists)
                {
                    throw new FaultException("A record was not created or updated because a duplicate of the current record already exists.");
                }

                var contact = new Entity
                {
                    Id = Guid.NewGuid(),
                    LogicalName = "contact",
                };

                createdEntities.Add(contact.ToEntityReference());
            }

            if (request.CreateOpportunity)
            {
                if (request.OpportunityCustomerId != null)
                {
                    if (request.OpportunityCustomerId.LogicalName != "account" &&
                        request.OpportunityCustomerId.LogicalName != "contact")
                    {
                        throw new FaultException($"CustomerIdType for opportunity can either be an account or contact.");
                    }

                    var customer = db.GetDbRow(request.OpportunityCustomerId);

                    if (customer == null)
                    {
                        throw new FaultException($"{customerMetadata.DisplayName} With Id = {request.OpportunityCustomerId.Id} Does Not Exist");
                    }
                }

                var opportunity = new Entity
                {
                    Id = Guid.NewGuid(),
                    LogicalName = "opportunity"
                };

                createdEntities.Add(opportunity.ToEntityReference());
            }

            var resp = new QualifyLeadResponse();
            resp.Results["CreatedEntities"] = createdEntities;
            return resp;
        }
    }
}
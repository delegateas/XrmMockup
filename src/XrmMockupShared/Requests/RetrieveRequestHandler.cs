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
using WorkflowExecuter;

namespace DG.Tools.XrmMockup {
    internal class RetrieveRequestHandler : RequestHandler {
        internal RetrieveRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Retrieve") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);
            

            if (request.Target.LogicalName == null) {
                throw new FaultException("You must provide a LogicalName");
            }


            if (request.ColumnSet == null && request.Target.KeyAttributes.Count == 0) {
                throw new FaultException("The columnset parameter must not be null when no KeyAttributes are provided");
            }

            var row = db.GetDbRow(request.Target);

            if (!security.HasPermission(row.ToEntity(), AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{row.Table.TableName}'");
            }

            core.ExecuteCalculatedFields(row);

            row = db.GetDbRow(request.Target);

            // Calculate the formula fields before we filter the fetched columns
            var looseEntity = row.ToEntity();
            core.ExecuteFormulaFields(row.Metadata, looseEntity).GetAwaiter().GetResult();

            var entity = core.GetStronglyTypedEntity(looseEntity, row.Metadata, request.ColumnSet);

            Utility.SetFormattedValues(db, entity, row.Metadata);

            if (!settings.SetUnsettableFields) {
                Utility.RemoveUnsettableAttributes("Retrieve", row.Metadata, entity);
            }

            Utility.HandlePrecision(metadata, db, entity);
            if (request.RelatedEntitiesQuery != null) {
                core.AddRelatedEntities(entity, request.RelatedEntitiesQuery, userRef);
            }

            var resp = new RetrieveResponse();
            resp.Results["Entity"] = entity;
            return resp;
        }
    }
}

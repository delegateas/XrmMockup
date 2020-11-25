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


#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (request.ColumnSet == null && request.Target.KeyAttributes.Count == 0) {
                throw new FaultException("The columnset parameter must not be null when no KeyAttributes are provided");
            }
#else
            if (request.ColumnSet == null) {
                throw new FaultException("The columnset parameter must not be null");
            }
#endif
            var row = db.GetDbRow(request.Target);

            if (!security.HasPermission(row.ToEntity(), AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{row.Table.TableName}'");
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            ExecuteCalculatedFields(row);
#endif
            row = db.GetDbRow(request.Target);
            var entity = core.GetStronglyTypedEntity(row.ToEntity(), row.Metadata, request.ColumnSet);

            Utility.SetFormmattedValues(db, entity, row.Metadata);

            if (!settings.SetUnsettableFields) {
                Utility.RemoveUnsettableAttributes("Retrieve", row.Metadata, entity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            Utility.HandlePrecision(metadata, db, entity);
#endif
            if (request.RelatedEntitiesQuery != null) {
                core.AddRelatedEntities(entity, request.RelatedEntitiesQuery, userRef);
            }
            
            var resp = new RetrieveResponse();
            resp.Results["Entity"] = entity;
            return resp;
        }


#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        private void ExecuteCalculatedFields(DbRow row) {
            var attributes = row.Metadata.Attributes.Where(
                m => m.SourceType == 1 && !(m is MoneyAttributeMetadata && m.LogicalName.EndsWith("_base")));

            foreach (var attr in attributes) {
                string definition = (attr as BooleanAttributeMetadata)?.FormulaDefinition;
                if (attr is BooleanAttributeMetadata) definition = (attr as BooleanAttributeMetadata).FormulaDefinition;
                else if (attr is DateTimeAttributeMetadata) definition = (attr as DateTimeAttributeMetadata).FormulaDefinition;
                else if (attr is DecimalAttributeMetadata) definition = (attr as DecimalAttributeMetadata).FormulaDefinition;
                else if (attr is IntegerAttributeMetadata) definition = (attr as IntegerAttributeMetadata).FormulaDefinition;
                else if (attr is MoneyAttributeMetadata) definition = (attr as MoneyAttributeMetadata).FormulaDefinition;
                else if (attr is PicklistAttributeMetadata) definition = (attr as PicklistAttributeMetadata).FormulaDefinition;
                else if (attr is StringAttributeMetadata) definition = (attr as StringAttributeMetadata).FormulaDefinition;

                if (definition == null) {
                    var trace = core.ServiceFactory.GetService(typeof(ITracingService)) as ITracingService;
                    trace.Trace($"Calculated field on {attr.EntityLogicalName} field {attr.LogicalName} is empty");
                    return;
                }
                var tree = WorkflowConstructor.ParseCalculated(definition);
                var factory = core.ServiceFactory;
                tree.Execute(row.ToEntity().CloneEntity(row.Metadata, new ColumnSet(true)), core.TimeOffset, core.GetWorkflowService(), 
                    factory, factory.GetService(typeof(ITracingService)) as ITracingService);
            }
        }
#endif
    }
}

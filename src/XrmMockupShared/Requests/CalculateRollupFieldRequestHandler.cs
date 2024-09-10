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
    internal class CalculateRollupFieldRequestHandler : RequestHandler {
        internal CalculateRollupFieldRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CalculateRollupField") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CalculateRollupFieldRequest>(orgRequest);


            var dbEntity = db.GetEntity(request.Target);
            var metadata = this.metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName);
            var field = metadata.Attributes.FirstOrDefault(a => a.LogicalName == request.FieldName);
            if (field == null) {
                throw new FaultException($"Couldn't find the field '{request.FieldName}' on the entity '{dbEntity.LogicalName}'");
            } else {
                string definition = (field as BooleanAttributeMetadata)?.FormulaDefinition;
                if ((field as BooleanAttributeMetadata)?.SourceType == 2) definition = (field as BooleanAttributeMetadata).FormulaDefinition;
                else if ((field as DateTimeAttributeMetadata)?.SourceType == 2) definition = (field as DateTimeAttributeMetadata).FormulaDefinition;
                else if ((field as DecimalAttributeMetadata)?.SourceType == 2) definition = (field as DecimalAttributeMetadata).FormulaDefinition;
                else if ((field as IntegerAttributeMetadata)?.SourceType == 2) definition = (field as IntegerAttributeMetadata).FormulaDefinition;
                else if ((field as MoneyAttributeMetadata)?.SourceType == 2) definition = (field as MoneyAttributeMetadata).FormulaDefinition;
                else if ((field as PicklistAttributeMetadata)?.SourceType == 2) definition = (field as PicklistAttributeMetadata).FormulaDefinition;
                else if ((field as StringAttributeMetadata)?.SourceType == 2) definition = (field as StringAttributeMetadata).FormulaDefinition;

                if (definition == null) {
                    throw new FaultException($"Field '{request.FieldName}' on entity '{dbEntity.LogicalName}' is not a rollup field");
                } else {
                    var tree = WorkflowConstructor.ParseRollUp(definition);
                    var factory = core.ServiceFactory;
                    var resultTree = tree.Execute(dbEntity, core.TimeOffset, core.GetWorkflowService(), factory, factory.GetService<ITracingService>());
                    var resultLocaltion = ((resultTree.StartActivity as RollUp).Aggregation[1] as Aggregate).VariableName;
                    var result = resultTree.Variables[resultLocaltion];
                    if (result != null) {
                        dbEntity[request.FieldName] = result;
                    }
                }
            }
            Utility.HandleCurrencies(this.metadata, db, dbEntity);
            db.Update(dbEntity);

            var resp = new CalculateRollupFieldResponse();
            resp.Results["Entity"] = db.GetEntity(request.Target);
            return resp;
        }
    }
}

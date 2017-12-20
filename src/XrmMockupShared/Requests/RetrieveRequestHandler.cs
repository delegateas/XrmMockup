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

            SetFormmattedValues(entity, row.Metadata);

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
                    throw new NotImplementedException("Unknown type when parsing calculated attr");
                }
                var tree = WorkflowConstructor.ParseCalculated(definition);
                var factory = core.ServiceFactory;
                tree.Execute(row.ToEntity().CloneEntity(row.Metadata, new ColumnSet(true)), core.TimeOffset, core.GetWorkflowService(), 
                    factory, factory.GetService(typeof(ITracingService)) as ITracingService);
            }
        }
#endif

        private Boolean IsValidForFormattedValues(AttributeMetadata attributeMetadata) {
            return
                attributeMetadata is PicklistAttributeMetadata ||
                attributeMetadata is BooleanAttributeMetadata ||
                attributeMetadata is MoneyAttributeMetadata ||
                attributeMetadata is LookupAttributeMetadata ||
                attributeMetadata is IntegerAttributeMetadata ||
                attributeMetadata is DateTimeAttributeMetadata ||
                attributeMetadata is MemoAttributeMetadata ||
                attributeMetadata is DoubleAttributeMetadata ||
                attributeMetadata is DecimalAttributeMetadata;
        }

        private string GetFormattedValueLabel(AttributeMetadata metadataAtt, object value, Entity entity) {
            if (metadataAtt is PicklistAttributeMetadata) {
                var optionset = (metadataAtt as PicklistAttributeMetadata).OptionSet.Options
                    .Where(opt => opt.Value == (value as OptionSetValue).Value).FirstOrDefault();
                return optionset.Label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is BooleanAttributeMetadata) {
                var booleanOptions = (metadataAtt as BooleanAttributeMetadata).OptionSet;
                var label = (bool)value ? booleanOptions.TrueOption.Label : booleanOptions.FalseOption.Label;
                return label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is MoneyAttributeMetadata) {
                var currencysymbol = 
                    db.GetEntity(
                        db.GetEntity(entity.ToEntityReference())
                        .GetAttributeValue<EntityReference>("transactioncurrencyid"))
                    .GetAttributeValue<string>("currencysymbol");
                
                return currencysymbol + (value as Money).Value.ToString();
            }

            if (metadataAtt is LookupAttributeMetadata) {
                try {
                    return (value as EntityReference).Name;
                } catch (NullReferenceException e) {
                    Console.WriteLine("No lookup entity exists: " + e.Message);
                }
            }

            if (metadataAtt is IntegerAttributeMetadata ||
                metadataAtt is DateTimeAttributeMetadata ||
                metadataAtt is MemoAttributeMetadata ||
                metadataAtt is DoubleAttributeMetadata ||
                metadataAtt is DecimalAttributeMetadata) {
                return value.ToString();
            }

            return null;
        }

        private void SetFormmattedValues(Entity entity, EntityMetadata metadata) {
            var validMetadata = metadata.Attributes
                .Where(a => IsValidForFormattedValues(a));

            var formattedValues = new List<KeyValuePair<string, string>>();
            foreach (var a in entity.Attributes) {
                if (a.Value == null) continue;
                var metadataAtt = validMetadata.Where(m => m.LogicalName == a.Key).FirstOrDefault();
                var formattedValuePair = new KeyValuePair<string, string>(a.Key, GetFormattedValueLabel(metadataAtt, a.Value, entity));
                if (formattedValuePair.Value != null) {
                    formattedValues.Add(formattedValuePair);
                }
            }

            if (formattedValues.Count > 0) {
                entity.FormattedValues.AddRange(formattedValues);
            }
        }
    }
}

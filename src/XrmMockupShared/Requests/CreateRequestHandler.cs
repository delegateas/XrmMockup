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

namespace DG.Tools.XrmMockup {
    internal class CreateRequestHandler : RequestHandler {
        internal CreateRequestHandler(Core core, ref DataMethods datamethods) : base(core, ref datamethods, "Create") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CreateRequest>(orgRequest);
            var resp = new CreateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            var entity = request.Target;
            if (entity.LogicalName == null) throw new MockupException("Entity needs a logical name");

            var EntityMetadata = dataMethods.GetMetadata(entity.LogicalName);
            var clonedEntity = entity.CloneEntity(EntityMetadata, new ColumnSet(true));
            var validAttributes = clonedEntity.Attributes.Where(x => x.Value != null);
            clonedEntity.Attributes = new AttributeCollection();
            clonedEntity.Attributes.AddRange(validAttributes);


            if (userRef != null && userRef.Id != Guid.Empty && !dataMethods.HasPermission(clonedEntity, AccessRights.CreateAccess, userRef)) {
                throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have create access for that entity");
            }

            if (dataMethods.HasCircularReference(clonedEntity)) {
                throw new FaultException($"Trying to create entity '{clonedEntity.LogicalName}', but the attributes had a circular reference");
            }

            var transactioncurrencyId = "transactioncurrencyid";
            var exchangerate = "exchangerate";
            if (!clonedEntity.Attributes.ContainsKey(transactioncurrencyId) &&
                Utility.IsSettableAttribute(transactioncurrencyId, EntityMetadata) &&
                EntityMetadata.Attributes.Any(m => m is MoneyAttributeMetadata) &&
                (settings.ServiceRole == MockupServiceSettings.Role.UI ||
                (settings.ServiceRole == MockupServiceSettings.Role.SDK && clonedEntity.Attributes.Any(
                    attr => EntityMetadata.Attributes.Where(a => a is MoneyAttributeMetadata).Any(m => m.LogicalName == attr.Key))))) {
                var user = dataMethods.GetEntityOrNull(userRef);
                if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                    clonedEntity.Attributes[transactioncurrencyId] = user[transactioncurrencyId];
                } else {
                    clonedEntity.Attributes[transactioncurrencyId] = dataMethods.baseCurrency;
                }
            }

            if (!clonedEntity.Attributes.ContainsKey(exchangerate) &&
                Utility.IsSettableAttribute(exchangerate, EntityMetadata) &&
                clonedEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                var currencyId = clonedEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = dataMethods.GetEntityOrNull(currencyId);
                clonedEntity.Attributes[exchangerate] = currency["exchangerate"];
                dataMethods.HandleCurrencies(clonedEntity);
            }

            var attributes = EntityMetadata.Attributes;
            if (!clonedEntity.Attributes.ContainsKey("statecode") &&
                Utility.IsSettableAttribute("statecode", EntityMetadata)) {
                var stateMeta = attributes.First(a => a.LogicalName == "statecode") as StateAttributeMetadata;
                clonedEntity["statecode"] = stateMeta.DefaultFormValue.HasValue ? new OptionSetValue(stateMeta.DefaultFormValue.Value) : new OptionSetValue(0);
            }
            if (!clonedEntity.Attributes.ContainsKey("statuscode") &&
                Utility.IsSettableAttribute("statuscode", EntityMetadata)) {
                var statusMeta = attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as StatusAttributeMetadata;
                clonedEntity["statuscode"] = statusMeta.DefaultFormValue.HasValue ? new OptionSetValue(statusMeta.DefaultFormValue.Value) : new OptionSetValue(1);
            }

            if (Utility.IsSettableAttribute("createdon", EntityMetadata)) {
                clonedEntity["createdon"] = DateTime.Now.Add(dataMethods.TimeOffset);
            }
            if (Utility.IsSettableAttribute("createdby", EntityMetadata)) {
                clonedEntity["createdby"] = userRef;
            }

            if (Utility.IsSettableAttribute("modifiedon", EntityMetadata) &&
                Utility.IsSettableAttribute("modifiedby", EntityMetadata)) {
                clonedEntity["modifiedon"] = clonedEntity["createdon"];
                clonedEntity["modifiedby"] = clonedEntity["createdby"];
            }

            var owner = userRef;
            if (clonedEntity.Attributes.ContainsKey("ownerid")) {
                owner = clonedEntity.GetAttributeValue<EntityReference>("ownerid");
            }
            dataMethods.SetOwner(clonedEntity, owner);

            if (!clonedEntity.Attributes.ContainsKey("businessunitid") &&
                clonedEntity.LogicalName == LogicalNames.SystemUser || clonedEntity.LogicalName == LogicalNames.Team) {
                clonedEntity["businessunitid"] = dataMethods.RootBusinessUnitRef;
            }

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit && !clonedEntity.Attributes.ContainsKey("parentbusinessunitid")) {
                clonedEntity["parentbusinessunitid"] = dataMethods.RootBusinessUnitRef;
            }
            if (settings.ServiceRole == MockupServiceSettings.Role.UI) {
                foreach (var attr in EntityMetadata.Attributes.Where(a => (a as BooleanAttributeMetadata)?.DefaultValue != null).ToList()) {
                    if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        clonedEntity[attr.LogicalName] = (attr as BooleanAttributeMetadata).DefaultValue;
                    }
                }

                foreach (var attr in EntityMetadata.Attributes.Where(a =>
                    (a as PicklistAttributeMetadata)?.DefaultFormValue != null && (a as PicklistAttributeMetadata)?.DefaultFormValue.Value != -1).ToList()) {
                    if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        clonedEntity[attr.LogicalName] = new OptionSetValue((attr as PicklistAttributeMetadata).DefaultFormValue.Value);
                    }
                }
            }

            if (clonedEntity.LogicalName == LogicalNames.Contact || clonedEntity.LogicalName == LogicalNames.Lead || clonedEntity.LogicalName == LogicalNames.SystemUser) {
                dataMethods.SetFullName(clonedEntity);
            }


            dataMethods.db.Add(clonedEntity);

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit) {
                dataMethods.AddRolesForBusinessUnit(clonedEntity.ToEntityReference());
            }

            if (entity.RelatedEntities.Count > 0) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    if (dataMethods.GetRelationshipMetadataDefaultNull(relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null) {
                        throw new FaultException($"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }
                    foreach (var relatedEntity in relatedEntities.Value.Entities) {
                        var req = new CreateRequest() {
                            Target = relatedEntity
                        };
                        core.Execute(req, userRef);
                    }
                    var associateReq = new AssociateRequest();
                    associateReq.Target = entity.ToEntityReference();
                    associateReq.Relationship = relatedEntities.Key;
                    associateReq.RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList());
                    core.Execute(associateReq, userRef);
                }
            }
            resp.Results.Add("id", clonedEntity.Id);
            return resp;
        }
    }
}

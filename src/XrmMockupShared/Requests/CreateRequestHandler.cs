﻿using System;
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

namespace DG.Tools.XrmMockup {
    internal class CreateRequestHandler : RequestHandler {
        internal CreateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Create") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CreateRequest>(orgRequest);
            var resp = new CreateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            var entity = request.Target;
            if (entity.LogicalName == null) throw new MockupException("Entity needs a logical name");

            var entityMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName);
            var clonedEntity = entity.CloneEntity(entityMetadata, new ColumnSet(true));
            var validAttributes = clonedEntity.Attributes.Where(x => x.Value != null);
            clonedEntity.Attributes = new AttributeCollection();
            clonedEntity.Attributes.AddRange(validAttributes);


            if (userRef != null && userRef.Id != Guid.Empty && !security.HasPermission(clonedEntity, AccessRights.CreateAccess, userRef)) {
                throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have create access for that entity");
            }

            if (Utility.HasCircularReference(metadata.EntityMetadata, clonedEntity)) {
                throw new FaultException($"Trying to create entity '{clonedEntity.LogicalName}', but the attributes had a circular reference");
            }

            var transactioncurrencyId = "transactioncurrencyid";
            var exchangerate = "exchangerate";
            if (!clonedEntity.Attributes.ContainsKey(transactioncurrencyId) &&
                Utility.IsSettableAttribute(transactioncurrencyId, entityMetadata) &&
                entityMetadata.Attributes.Any(m => m is MoneyAttributeMetadata) &&
                (settings.ServiceRole == MockupServiceSettings.Role.UI ||
                (settings.ServiceRole == MockupServiceSettings.Role.SDK && clonedEntity.Attributes.Any(
                    attr => entityMetadata.Attributes.Where(a => a is MoneyAttributeMetadata).Any(m => m.LogicalName == attr.Key))))) {
                var user = db.GetEntityOrNull(userRef);
                if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                    clonedEntity.Attributes[transactioncurrencyId] = user[transactioncurrencyId];
                } else {
                    clonedEntity.Attributes[transactioncurrencyId] = Utility.GetBaseCurrency(metadata);
                }
            }

            if (!clonedEntity.Attributes.ContainsKey(exchangerate) &&
                Utility.IsSettableAttribute(exchangerate, entityMetadata) &&
                clonedEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                var currencyId = clonedEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db.GetEntityOrNull(currencyId);
                clonedEntity.Attributes[exchangerate] = currency["exchangerate"];
                Utility.HandleCurrencies(metadata, db, clonedEntity);
            }

            var attributes = entityMetadata.Attributes;
            if (!clonedEntity.Attributes.ContainsKey("statecode") &&
                Utility.IsSettableAttribute("statecode", entityMetadata)) {
                var stateMeta = attributes.First(a => a.LogicalName == "statecode") as StateAttributeMetadata;
                clonedEntity["statecode"] = stateMeta.DefaultFormValue.HasValue ? new OptionSetValue(stateMeta.DefaultFormValue.Value) : new OptionSetValue(0);
            }
            if (!clonedEntity.Attributes.ContainsKey("statuscode") &&
                Utility.IsSettableAttribute("statuscode", entityMetadata)) {
                var statusMeta = attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as StatusAttributeMetadata;
                clonedEntity["statuscode"] = statusMeta.DefaultFormValue.HasValue ? new OptionSetValue(statusMeta.DefaultFormValue.Value) : new OptionSetValue(1);
            }

            if (Utility.IsSettableAttribute("createdon", entityMetadata)) {
                clonedEntity["createdon"] = DateTime.Now.Add(core.TimeOffset);
            }
            if (Utility.IsSettableAttribute("createdby", entityMetadata)) {
                clonedEntity["createdby"] = userRef;
            }

            if (Utility.IsSettableAttribute("modifiedon", entityMetadata) &&
                Utility.IsSettableAttribute("modifiedby", entityMetadata)) {
                clonedEntity["modifiedon"] = clonedEntity["createdon"];
                clonedEntity["modifiedby"] = clonedEntity["createdby"];
            }

            var owner = userRef;
            if (clonedEntity.Attributes.ContainsKey("ownerid")) {
                owner = clonedEntity.GetAttributeValue<EntityReference>("ownerid");
            }
            Utility.SetOwner(db, security, metadata, clonedEntity, owner);

            if (!clonedEntity.Attributes.ContainsKey("businessunitid") &&
                clonedEntity.LogicalName == LogicalNames.SystemUser || clonedEntity.LogicalName == LogicalNames.Team) {
                clonedEntity["businessunitid"] = metadata.RootBusinessUnit.ToEntityReference();
            }

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit && !clonedEntity.Attributes.ContainsKey("parentbusinessunitid")) {
                clonedEntity["parentbusinessunitid"] = metadata.RootBusinessUnit.ToEntityReference();
            }
            if (settings.ServiceRole == MockupServiceSettings.Role.UI) {
                foreach (var attr in entityMetadata.Attributes.Where(a => (a as BooleanAttributeMetadata)?.DefaultValue != null).ToList()) {
                    if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        clonedEntity[attr.LogicalName] = (attr as BooleanAttributeMetadata).DefaultValue;
                    }
                }

                foreach (var attr in entityMetadata.Attributes.Where(a =>
                    (a as PicklistAttributeMetadata)?.DefaultFormValue != null && (a as PicklistAttributeMetadata)?.DefaultFormValue.Value != -1).ToList()) {
                    if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        clonedEntity[attr.LogicalName] = new OptionSetValue((attr as PicklistAttributeMetadata).DefaultFormValue.Value);
                    }
                }
            }

            if (clonedEntity.LogicalName == LogicalNames.Contact || clonedEntity.LogicalName == LogicalNames.Lead || clonedEntity.LogicalName == LogicalNames.SystemUser) {
                Utility.SetFullName(metadata, clonedEntity);
            }

            if (Utility.Activities.Contains(clonedEntity.LogicalName)) {
                clonedEntity["activitytypecode"] = Utility.ActivityTypeCode[clonedEntity.LogicalName];

                var req = new CreateRequest {
                    Target = clonedEntity.ToActivityPointer()
                };
                core.Execute(req, userRef);
            }
            
            db.Add(clonedEntity);

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit) {
                security.AddRolesForBusinessUnit(db, clonedEntity.ToEntityReference());
            }

            if (entity.RelatedEntities.Count > 0) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    if (Utility.GetRelationshipMetadataDefaultNull(metadata.EntityMetadata, relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null) {
                        throw new FaultException($"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }
                    foreach (var relatedEntity in relatedEntities.Value.Entities) {
                        var req = new CreateRequest() {
                            Target = relatedEntity
                        };
                        core.Execute(req, userRef);
                    }
                    var associateReq = new AssociateRequest {
                        Target = entity.ToEntityReference(),
                        Relationship = relatedEntities.Key,
                        RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList())
                    };
                    core.Execute(associateReq, userRef);
                }
            }
            resp.Results.Add("id", clonedEntity.Id);
            return resp;
        }
    }
}

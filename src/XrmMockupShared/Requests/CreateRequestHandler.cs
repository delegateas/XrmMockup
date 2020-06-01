using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class CreateRequestHandler : RequestHandler
    {
        internal CreateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core,
            db, metadata, security, "Create")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
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

            SecurityChecks(userRef, clonedEntity, entity, settings);

            if (Utility.HasCircularReference(metadata.EntityMetadata, clonedEntity))
            {
                throw new FaultException(
                    $"Trying to create entity '{clonedEntity.LogicalName}', but the attributes had a circular reference");
            }

            HandleCurrency(userRef, clonedEntity, entityMetadata, settings);

            HandleAutoNumberAttributes(clonedEntity, entityMetadata);

            HandleStateAndStatus(entityMetadata, clonedEntity);

            if (Utility.IsValidAttribute("createdon", entityMetadata))
            {
                clonedEntity["createdon"] = DateTime.UtcNow.Add(core.TimeOffset);
            }

            if (Utility.IsValidAttribute("createdby", entityMetadata))
            {
                clonedEntity["createdby"] = userRef;
            }

            if (Utility.IsValidAttribute("modifiedon", entityMetadata) &&
                Utility.IsValidAttribute("modifiedby", entityMetadata))
            {
                clonedEntity["modifiedon"] = clonedEntity["createdon"];
                clonedEntity["modifiedby"] = clonedEntity["createdby"];
            }

            var owner = userRef;
            if (clonedEntity.Attributes.ContainsKey("ownerid"))
            {
                owner = clonedEntity.GetAttributeValue<EntityReference>("ownerid");
            }

            Utility.SetOwner(db, security, metadata, clonedEntity, owner);

            if (!clonedEntity.Attributes.ContainsKey("businessunitid") &&
                (clonedEntity.LogicalName == LogicalNames.SystemUser || clonedEntity.LogicalName == LogicalNames.Team))
            {
                clonedEntity["businessunitid"] = metadata.RootBusinessUnit.ToEntityReference();
            }

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit)
            {
                CheckBusinessUnitAttributes(clonedEntity, settings);
            }

            foreach (var attr in entityMetadata.Attributes
                .Where(a => (a as BooleanAttributeMetadata)?.DefaultValue != null).ToList())
            {
                if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName))
                {
                    clonedEntity[attr.LogicalName] = (attr as BooleanAttributeMetadata).DefaultValue;
                }
            }

            foreach (var attr in entityMetadata.Attributes.Where(a =>
                (a as PicklistAttributeMetadata)?.DefaultFormValue != null &&
                (a as PicklistAttributeMetadata)?.DefaultFormValue.Value != -1).ToList())
            {
                if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName))
                {
                    clonedEntity[attr.LogicalName] =
                        new OptionSetValue((attr as PicklistAttributeMetadata).DefaultFormValue.Value);
                }
            }

            if (clonedEntity.LogicalName == LogicalNames.Contact || clonedEntity.LogicalName == LogicalNames.Lead ||
                clonedEntity.LogicalName == LogicalNames.SystemUser)
            {
                Utility.SetFullName(metadata, clonedEntity);
            }

            clonedEntity.Attributes
                .Where(x => x.Value is string && x.Value != null && string.IsNullOrEmpty((string) x.Value))
                .ToList()
                .ForEach(x => clonedEntity[x.Key] = null);

            if (Utility.Activities.Contains(clonedEntity.LogicalName))
            {
                clonedEntity["activitytypecode"] = Utility.ActivityTypeCode[clonedEntity.LogicalName];

                var req = new CreateRequest
                {
                    Target = clonedEntity.ToActivityPointer()
                };
                core.Execute(req, userRef);
            }

            db.Add(clonedEntity);

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit)
            {
                security.AddRolesForBusinessUnit(db, clonedEntity.ToEntityReference());
                CreateDefaultTeamForBusinessUnit(clonedEntity, userRef);
            }

            if (entity.RelatedEntities.Count > 0)
            {
                foreach (var relatedEntities in entity.RelatedEntities)
                {
                    if (Utility.GetRelationshipMetadataDefaultNull(metadata.EntityMetadata,
                        relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null)
                    {
                        throw new FaultException(
                            $"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }

                    foreach (var relatedEntity in relatedEntities.Value.Entities)
                    {
                        var req = new CreateRequest()
                        {
                            Target = relatedEntity
                        };
                        core.Execute(req, userRef);
                    }

                    var associateReq = new AssociateRequest
                    {
                        Target = entity.ToEntityReference(),
                        Relationship = relatedEntities.Key,
                        RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities
                            .Select(e => e.ToEntityReference()).ToList())
                    };
                    core.Execute(associateReq, userRef);
                }
            }

            resp.Results.Add("id", clonedEntity.Id);
            return resp;
        }

        private void HandleAutoNumberAttributes(Entity clonedEntity, EntityMetadata entityMetadata)
        {
#if XRM_MOCKUP_365
            foreach (var metadataAttribute in entityMetadata.Attributes)
            {
                if (string.IsNullOrEmpty(metadataAttribute.AutoNumberFormat)) continue;

                var autoNumberFormat = metadataAttribute.AutoNumberFormat;

                var autoValue = autoNumberFormat;

                var formats = Regex.Matches(autoNumberFormat, @"{(.+?)}");

                foreach (Match format in formats)
                {
                    autoValue = autoValue.Replace(format.Value, DetermineAutoNumberValue(format.Value, metadataAttribute));
                }

                clonedEntity.Attributes[metadataAttribute.LogicalName] = autoValue;
            }
#endif
        }

        private string DetermineAutoNumberValue(string format, AttributeMetadata metadataAttribute)
        {
            if (format.Contains("SEQNUM"))
            {
                var currentNumber = 1000L; // Default value for Auto Number seed
                var key = metadataAttribute.EntityLogicalName + metadataAttribute.LogicalName;
                if (core.AutoNumberValues.ContainsKey(key))
                {
                    currentNumber = core.AutoNumberValues[key];
                }

                core.AutoNumberValues[key] = currentNumber + 1;

                return currentNumber.ToString($"D{format[8]}");
            }

            if (format.Contains("RANDSTRING"))
            {
                var rand = new Random();
                const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var length = (int) char.GetNumericValue(format[12]);
                var randomString = "";

                for (var i = 0; i < length; i++)
                {
                    randomString += characters[rand.Next(characters.Length)] + "";
                }

                return randomString;
            }

            if (format.Contains("DATETIMEUTC"))
            {
                var datetime = DateTime.Now;
                var datetimeFormat = Regex.Match(format, @":(.+?)}");
                return datetime.ToString(datetimeFormat.Groups[1].Value);
            }

            return "";
        }

        private void HandleCurrency(EntityReference userRef, Entity clonedEntity, EntityMetadata entityMetadata,
            MockupServiceSettings settings)
        {
            var transactioncurrencyId = "transactioncurrencyid";
            var exchangerate = "exchangerate";

            if (!clonedEntity.Attributes.ContainsKey(transactioncurrencyId) &&
                Utility.IsValidAttribute(transactioncurrencyId, entityMetadata) &&
                entityMetadata.Attributes.Any(m => m is MoneyAttributeMetadata) &&
                (settings.ServiceRole == MockupServiceSettings.Role.UI ||
                 (settings.ServiceRole == MockupServiceSettings.Role.SDK && clonedEntity.Attributes.Any(
                     attr => entityMetadata.Attributes.Where(a => a is MoneyAttributeMetadata)
                         .Any(m => m.LogicalName == attr.Key)))))
            {
                var user = db.GetEntityOrNull(userRef);
                if (user.Attributes.ContainsKey(transactioncurrencyId))
                {
                    clonedEntity.Attributes[transactioncurrencyId] = user[transactioncurrencyId];
                }
                else
                {
                    clonedEntity.Attributes[transactioncurrencyId] = Utility.GetBaseCurrency(metadata);
                }
            }

            if (clonedEntity.Attributes.ContainsKey(exchangerate) ||
                !Utility.IsValidAttribute(exchangerate, entityMetadata) ||
                !clonedEntity.Attributes.ContainsKey(transactioncurrencyId)) return;

            var currencyId = clonedEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
            var currency = db.GetEntityOrNull(currencyId);
            clonedEntity.Attributes[exchangerate] = currency["exchangerate"];
            Utility.HandleCurrencies(metadata, db, clonedEntity);
        }

        private void HandleStateAndStatus(EntityMetadata entityMetadata, Entity clonedEntity)
        {
            if (!Utility.IsValidAttribute("statecode", entityMetadata) ||
                !Utility.IsValidAttribute("statuscode", entityMetadata)) return;

            var defaultState = 0;
            try
            {
                var defaultStateStatus = metadata.DefaultStateStatus[clonedEntity.LogicalName];
                if (!clonedEntity.Attributes.ContainsKey("statecode") &&
                    !clonedEntity.Attributes.ContainsKey("statuscode"))
                {
                    clonedEntity["statecode"] = new OptionSetValue(defaultState);
                    clonedEntity["statuscode"] = new OptionSetValue(defaultStateStatus[defaultState]);
                }
                else
                {
                    var statusmeta =
                        (entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as
                            StatusAttributeMetadata)
                        ?.OptionSet.Options
                        .Cast<StatusOptionMetadata>()
                        .FirstOrDefault(o =>
                            o.Value == clonedEntity.GetAttributeValue<OptionSetValue>("statuscode")?.Value);
                    if (clonedEntity.LogicalName != "opportunityclose" && // is allowed to be created inactive 
                        ((clonedEntity.Attributes.ContainsKey("statecode") &&
                          clonedEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value != defaultState) ||
                         (clonedEntity.Attributes.ContainsKey("statuscode") && statusmeta?.State != defaultState)))
                    {
                        clonedEntity["statecode"] = new OptionSetValue(defaultState);
                        clonedEntity["statuscode"] = new OptionSetValue(defaultStateStatus[defaultState]);
                    }
                    else if (!clonedEntity.Contains("statecode") ||
                             clonedEntity.GetAttributeValue<OptionSetValue>("statecode") == null)
                    {
                        clonedEntity["statecode"] = new OptionSetValue(statusmeta.State.Value);
                    }
                    else if (!clonedEntity.Contains("statuscode") ||
                             clonedEntity.GetAttributeValue<OptionSetValue>("statuscode") == null)
                    {
                        clonedEntity["statuscode"] = new OptionSetValue(defaultStateStatus[defaultState]);
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException(
                    $"Unable to get default status reason for the state {defaultState.ToString()} in {clonedEntity.LogicalName} entity. " +
                    $"This might be due to unsaved default status reason changes. Please update, save, and publish the relevant status reason field on {clonedEntity.LogicalName} and generate new metadata");
            }
        }

        private void SecurityChecks(EntityReference userRef, Entity clonedEntity, Entity entity,
            MockupServiceSettings settings)
        {
            if (userRef == null || userRef.Id == Guid.Empty) return;

            if (!security.HasPermission(clonedEntity, AccessRights.CreateAccess, userRef))
            {
                throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                                         $", but the calling user with id '{userRef.Id}' does not have Create access for that entity");
            }

            if (!core.GetMockupSettings().AppendAndAppendToPrivilegeCheck.GetValueOrDefault(true)) return;

            var references = clonedEntity.Attributes
                .Where(x => x.Value is EntityReference && x.Key != "ownerid")
                .ToArray();

            if (references.Any())
            {
                if (!security.HasPermission(clonedEntity, AccessRights.AppendAccess, userRef))
                {
                    throw new FaultException($"Trying to create entity '{entity.LogicalName}' with references" +
                                             $", but the calling user with id '{userRef.Id}' does not have Append access for that entity");
                }
            }

            foreach (var attr in references)
            {
                var reference = attr.Value as EntityReference;
                if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                    !security.HasPermission(reference, AccessRights.ReadAccess, userRef))
                {
                    throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                                             $", but the calling user with id '{userRef.Id}' does not have read access for referenced entity '{reference.LogicalName}' on attribute '{attr.Key}'");
                }

                if (!security.HasPermission(reference, AccessRights.AppendToAccess, userRef))
                {
                    throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                                             $", but the calling user with id '{userRef.Id}' does not have AppendTo access for referenced entity '{reference.LogicalName}' on attribute '{attr.Key}'");
                }
            }
        }

        private void CheckBusinessUnitAttributes(Entity clonedEntity, MockupServiceSettings settings)
        {
            if (!clonedEntity.Attributes.ContainsKey("parentbusinessunitid"))
            {
                if (settings.ServiceRole == MockupServiceSettings.Role.UI)
                {
                    clonedEntity["parentbusinessunitid"] = metadata.RootBusinessUnit.ToEntityReference();
                }
                else
                {
                    throw new FaultException("Only one organization and one root business are allowed.");
                }
            }

            if (!clonedEntity.Attributes.ContainsKey("name"))
            {
                throw new FaultException("Business unit must have a name.");
                // The real error :  Condition for attribute 'businessunit.name': null is not a valid value for an attribute. Use 'Null' or 'NotNull' conditions instead.
            }
        }

        private void CreateDefaultTeamForBusinessUnit(Entity clonedEntity, EntityReference userRef)
        {
            var req = new CreateRequest
            {
                Target = Utility.CreateDefaultTeam(clonedEntity, userRef),
                Parameters =
                {
                    [MockupExecutionContext.Key] =
                        new MockupServiceSettings(true, true, MockupServiceSettings.Role.SDK)
                }
            };
            core.Execute(req, userRef);
        }
    }
}
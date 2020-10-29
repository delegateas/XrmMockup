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

namespace DG.Tools.XrmMockup
{
    internal class UpdateRequestHandler : RequestHandler
    {
        internal UpdateRequestHandler(Core core, IXrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Update") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpdateRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);

            var entRef = request.Target.ToEntityReferenceWithKeyAttributes();
            var entity = request.Target;
            var row = db.GetDbRow(entRef);

            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName != LogicalNames.Opportunity &&
                row.Table.TableName != LogicalNames.SystemUser &&
                row.GetColumn<int?>("statecode") == 1)
            {
                throw new MockupException($"Trying to update inactive '{row.Table.TableName}', which is impossible in UI");
            }

            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName == LogicalNames.Opportunity &&
                row.GetColumn<int?>("statecode") == 1)
            {
                throw new MockupException($"Trying to update closed opportunity '{row.Id}', which is impossible in UI");
            }


            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName == LogicalNames.SystemUser &&
                row.GetColumn<bool?>("isdisabled") == true)
            {
                throw new MockupException($"Trying to update inactive systemuser '{row.Id}', which is impossible in UI");
            }

            // modify for all activites
            //if (entity.LogicalName == "activity" && dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1) return;
            var xrmEntity = row.ToEntity();

            if (!security.HasPermission(xrmEntity, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException($"Trying to update entity '{row.Table.TableName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }

            if (core.GetMockupSettings().AppendAndAppendToPrivilegeCheck.GetValueOrDefault(true))
            {
                var references = entity.Attributes
                    .Where(x => x.Value is EntityReference && x.Key != "ownerid")
                    .ToArray();

                if (references.Any())
                {
                    if (!security.HasPermission(xrmEntity, AccessRights.AppendAccess, userRef))
                    {
                        throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}' with references" +
                            $", but the calling user with id '{userRef.Id}' does not have Append access for that entity");
                    }
                }

                foreach (var attr in references)
                {
                    var reference = attr.Value as EntityReference;
                    if (settings.ServiceRole == MockupServiceSettings.Role.UI && !security.HasPermission(reference, AccessRights.ReadAccess, userRef))
                    {
                        throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}'" +
                            $", but the calling user with id '{userRef.Id}' does not have read access for referenced entity '{reference.LogicalName}' on attribute '{attr.Key}'");
                    }
                    if (!security.HasPermission(reference, AccessRights.AppendToAccess, userRef))
                    {
                        throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}'" +
                            $", but the calling user with id '{userRef.Id}' does not have AppendTo access for referenced entity '{reference.LogicalName}' on attribute '{attr.Key}'");
                    }
                }
            }

            var ownerRef = request.Target.GetAttributeValue<EntityReference>("ownerid");
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (ownerRef != null)
            {
                security.CheckAssignPermission(xrmEntity, ownerRef, userRef);
            }
#endif

            var updEntity = request.Target.CloneEntity(row.Metadata, new ColumnSet(true));

            if (updEntity.Contains("statecode") || updEntity.Contains("statuscode"))
            {
                var defaultStateStatus = metadata.DefaultStateStatus[updEntity.LogicalName];
                var statusmeta =
                    (metadata.EntityMetadata.GetMetadata(updEntity.LogicalName)
                        .Attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as StatusAttributeMetadata)
                    ?.OptionSet.Options
                    .Cast<StatusOptionMetadata>()
                    .FirstOrDefault(o => o.Value == updEntity.GetAttributeValue<OptionSetValue>("statuscode")?.Value);

                if ((!updEntity.Contains("statecode") || updEntity.GetAttributeValue<OptionSetValue>("statecode") == null)
                    && statusmeta != null)
                {
                    updEntity["statecode"] = new OptionSetValue(statusmeta.State.Value);
                }
                else if (!updEntity.Contains("statuscode") || updEntity.GetAttributeValue<OptionSetValue>("statuscode") == null)
                {
                    var state = updEntity.GetAttributeValue<OptionSetValue>("statecode").Value;
                    updEntity["statuscode"] = new OptionSetValue(defaultStateStatus[state]);
                }
            }


#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            Utility.CheckStatusTransitions(row.Metadata, updEntity, xrmEntity);
#endif


            if (Utility.HasCircularReference(metadata.EntityMetadata, updEntity))
            {
                throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}', but the attributes had a circular reference");
            }

            if (updEntity.LogicalName == LogicalNames.Contact || updEntity.LogicalName == LogicalNames.Lead || updEntity.LogicalName == LogicalNames.SystemUser)
            {
                Utility.SetFullName(metadata, updEntity);
            }

            updEntity.Attributes
                .Where(x => x.Value is string && x.Value != null && string.IsNullOrEmpty((string)x.Value))
                .ToList()
                .ForEach(x => updEntity[x.Key] = null);

            xrmEntity.SetAttributes(updEntity.Attributes, metadata.EntityMetadata[updEntity.LogicalName]);

            var transactioncurrencyId = "transactioncurrencyid";
            if (updEntity.LogicalName != LogicalNames.TransactionCurrency &&
                (updEntity.Attributes.ContainsKey(transactioncurrencyId) ||
                updEntity.Attributes.Any(a => row.Metadata.Attributes.Any(m => m.LogicalName == a.Key && m is MoneyAttributeMetadata))))
            {
                if (!xrmEntity.Attributes.ContainsKey(transactioncurrencyId))
                {
                    var user = db.GetEntity(userRef);
                    if (user.Attributes.ContainsKey(transactioncurrencyId))
                    {
                        xrmEntity[transactioncurrencyId] = user[transactioncurrencyId];
                    }
                    else
                    {
                        xrmEntity[transactioncurrencyId] = core.baseCurrency;
                    }
                }
                var currencyId = xrmEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db.GetEntity(LogicalNames.TransactionCurrency, currencyId.Id);
                xrmEntity["exchangerate"] = currency.GetAttributeValue<decimal?>("exchangerate");
                Utility.HandleCurrencies(metadata, db, xrmEntity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (updEntity.Attributes.ContainsKey("statecode") || updEntity.Attributes.ContainsKey("statuscode"))
            {
                Utility.HandleCurrencies(metadata, db, xrmEntity);
            }
#endif

            if (ownerRef != null)
            {
                Utility.SetOwner(db, security, metadata, xrmEntity, ownerRef);
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
                security.CascadeOwnerUpdate(xrmEntity, userRef, ownerRef);
#endif
            }
            
            if (Utility.Activities.Contains(xrmEntity.LogicalName))
            {
                xrmEntity["activitytypecode"] = Utility.ActivityTypeCode[xrmEntity.LogicalName];

                var req = new UpdateRequest
                {
                    Target = xrmEntity.ToActivityPointer()
                };
                core.Execute(req, userRef);
            }

            Utility.Touch(xrmEntity, row.Metadata, core.TimeOffset, userRef);

            db.Update(xrmEntity);

            return new UpdateResponse();
        }
    }
}
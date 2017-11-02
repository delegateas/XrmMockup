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

namespace DG.Tools.XrmMockup {
    internal class UpdateRequestHandler : RequestHandler {
        internal UpdateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, DataMethods datamethods) : base(core, db, metadata, datamethods, "Update") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<UpdateRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);



            var entRef = request.Target.ToEntityReferenceWithKeyAttributes();
            var row = db.GetDbRow(entRef);

            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName != LogicalNames.Opportunity &&
                row.GetColumn<int?>("statecode") == 1) {
                throw new MockupException($"Trying to update inactive '{row.Table.TableName}', which is impossible in UI");
            }

            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName == LogicalNames.Opportunity &&
                row.GetColumn<int?>("statecode") == 1) {
                throw new MockupException($"Trying to update closed opportunity '{row.Id}', which is impossible in UI");
            }

            // modify for all activites
            //if (entity.LogicalName == "activity" && dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1) return;
            var xrmEntity = row.ToEntity();

            if (!dataMethods.HasPermission(row.ToEntity(), AccessRights.WriteAccess, userRef)) {
                throw new FaultException($"Trying to update entity '{row.Table.TableName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }

            var ownerRef = request.Target.GetAttributeValue<EntityReference>("ownerid");
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (ownerRef != null) {
                dataMethods.CheckAssignPermission(xrmEntity, ownerRef, userRef);
            }
#endif

            var updEntity = request.Target.CloneEntity(row.Metadata, new ColumnSet(true));
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            Utility.CheckStatusTransitions(row.Metadata, updEntity, xrmEntity);
#endif


            if (Utility.HasCircularReference(metadata.EntityMetadata, updEntity)) {
                throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}', but the attributes had a circular reference");
            }

            if (updEntity.LogicalName == LogicalNames.Contact || updEntity.LogicalName == LogicalNames.Lead || updEntity.LogicalName == LogicalNames.SystemUser) {
                Utility.SetFullName(metadata, updEntity);
            }

            xrmEntity.SetAttributes(updEntity.Attributes, metadata.EntityMetadata[updEntity.LogicalName]);

            var transactioncurrencyId = "transactioncurrencyid";
            if (updEntity.LogicalName != LogicalNames.TransactionCurrency &&
                (updEntity.Attributes.ContainsKey(transactioncurrencyId) ||
                updEntity.Attributes.Any(a => row.Metadata.Attributes.Any(m => m.LogicalName == a.Key && m is MoneyAttributeMetadata)))) {
                if (!xrmEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                    var user = db.GetEntity(userRef);
                    if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                        xrmEntity[transactioncurrencyId] = user[transactioncurrencyId];
                    } else {
                        xrmEntity[transactioncurrencyId] = dataMethods.baseCurrency;
                    }
                }
                var currencyId = xrmEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db.GetEntity(LogicalNames.TransactionCurrency, currencyId.Id);
                xrmEntity["exchangerate"] = currency.GetAttributeValue<decimal?>("exchangerate");
                Utility.HandleCurrencies(metadata, db, xrmEntity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (updEntity.Attributes.ContainsKey("statecode") || updEntity.Attributes.ContainsKey("statuscode")) {
                Utility.HandleCurrencies(metadata, db, xrmEntity);
            }
#endif

            if (ownerRef != null) {
                Utility.SetOwner(db, dataMethods, metadata, xrmEntity, ownerRef);
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
                dataMethods.CascadeOwnerUpdate(xrmEntity, userRef, ownerRef);
#endif
            }
            Utility.Touch(xrmEntity, row.Metadata, core.TimeOffset, userRef);

            db.Update(xrmEntity);
            
            return new UpdateResponse();
        }
    }
}
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
    internal class SetStateRequestHandler : RequestHandler {
        internal SetStateRequestHandler(Core core, IXrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SetState") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<SetStateRequest>(orgRequest);

            var record = db.GetEntity(request.EntityMoniker);
            if (Utility.IsValidAttribute("statecode", core.GetEntityMetadata(record.LogicalName)) &&
                Utility.IsValidAttribute("statuscode", core.GetEntityMetadata(record.LogicalName)))
                {
                var prevEntity = record.CloneEntity();
                record["statecode"] = request.State;
                record["statuscode"] = request.Status;
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
                Utility.CheckStatusTransitions(core.GetEntityMetadata(record.LogicalName), record, prevEntity);
#endif
                Utility.HandleCurrencies(metadata, db, record);
                Utility.Touch(record, core.GetEntityMetadata(record.LogicalName), core.TimeOffset, userRef);

                db.Update(record);
            }
            return new SetStateResponse();
        }
    }
}

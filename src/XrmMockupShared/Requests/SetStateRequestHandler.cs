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
        internal SetStateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, DataMethods datamethods) : base(core, db, metadata, datamethods, "SetState") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<SetStateRequest>(orgRequest);

            var row = db.GetDbRow(request.EntityMoniker);
            var record = row.ToEntity();
            if (Utility.IsSettableAttribute("statecode", row.Metadata) &&
                Utility.IsSettableAttribute("statuscode", row.Metadata)) {
                var prevEntity = record.CloneEntity();
                record["statecode"] = request.State;
                record["statuscode"] = request.Status;
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
                Utility.CheckStatusTransitions(row.Metadata, record, prevEntity);
#endif
                Utility.HandleCurrencies(metadata, db, record);
                Utility.Touch(record, row.Metadata, core.TimeOffset, userRef);

                db.Update(record);
            }
            return new SetStateResponse();
        }
    }
}

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015 || XRM_MOCKUP_2016)

using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;
using System.Linq;

namespace DG.Tools.XrmMockup
{
    internal class UploadBlockRequestHandler : RequestHandler {
        internal UploadBlockRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "UploadBlock") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<UploadBlockRequest>(orgRequest);
            
            // Document store not implemented in database yet

            var resp = new UploadBlockResponse();
            return resp;
        }
    }
}

#endif
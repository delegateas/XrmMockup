﻿using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;
using System.Linq;

namespace DG.Tools.XrmMockup
{
    internal class CommitFileBlocksUploadRequestHandler : RequestHandler {
        internal CommitFileBlocksUploadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CommitFileBlocksUpload") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CommitFileBlocksUploadRequest>(orgRequest);
            
            // Document store not implemented in database yet

            var resp = new UploadBlockResponse();
            return resp;
        }
    }
}

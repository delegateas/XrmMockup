using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;
using System.Linq;

namespace DG.Tools.XrmMockup
{
    internal class InitializeFileBlocksUploadRequestHandler : RequestHandler {
        internal InitializeFileBlocksUploadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "InitializeFileBlocksUpload") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<InitializeFileBlocksUploadRequest>(orgRequest);

            var fileAttachment = new Entity("fileattachment");
            fileAttachment["filename"] = request.FileName;
            fileAttachment["regardingfieldname"] = request.FileAttributeName;
            fileAttachment["objectid"] = request.Target;
            db.Add(fileAttachment);


            var resp = new InitializeFileBlocksUploadResponse();
            resp.Results["FileContinuationToken"] = Guid.NewGuid().ToString();
            return resp;
        }
    }
}

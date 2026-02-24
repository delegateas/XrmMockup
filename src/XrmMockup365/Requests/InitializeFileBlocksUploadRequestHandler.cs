using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System;

namespace DG.Tools.XrmMockup
{
    internal sealed class InitializeFileBlocksUploadRequestHandler : RequestHandler
    {
        internal InitializeFileBlocksUploadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "InitializeFileBlocksUpload") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InitializeFileBlocksUploadRequest>(orgRequest);

            var token = Guid.NewGuid().ToString();
            var fileAttachmentId = Guid.NewGuid();

            // Create the fileattachment entity in the database
            var fileAttachment = new Entity("fileattachment");
            fileAttachment["filename"] = request.FileName;
            fileAttachment["regardingfieldname"] = request.FileAttributeName;
            fileAttachment["objectid"] = request.Target;
            db.Add(fileAttachment);

            var session = new FileUploadSession
            {
                FileAttachmentId = fileAttachmentId,
                FileName = request.FileName,
                Target = request.Target,
                FileAttributeName = request.FileAttributeName,
                CreatedOn = DateTime.UtcNow
            };

            core.FileBlockStore.StartUpload(token, session);

            var resp = new InitializeFileBlocksUploadResponse();
            resp.Results["FileContinuationToken"] = token;
            return resp;
        }
    }
}
